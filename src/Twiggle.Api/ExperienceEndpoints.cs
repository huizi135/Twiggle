using Microsoft.EntityFrameworkCore;
namespace Twiggle.Api;

public static class ExperienceEndpoints
{
    public static void MapExperiences(this WebApplication app)
    {
        app.MapGet("/api/sessions", async (TwiggleDbContext db, CancellationToken ct) => Results.Ok(await db.Sessions
            .Where(x => x.WorkspaceId == LocalWorkspace.Id).OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.Title, x.Revision, x.CreatedAt, Saved = x.Episode != null }).ToListAsync(ct)));
        app.MapPost("/api/sessions", async (CreateSession input, TwiggleDbContext db, CancellationToken ct) =>
        {
            if (input.Id == Guid.Empty || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160)
                return Results.BadRequest(new { message = "Provide an operation ID and a title of 1–160 characters." });
            var existing = await db.Sessions.SingleOrDefaultAsync(x => x.Id == input.Id, ct);
            if (existing != null) return existing.WorkspaceId == LocalWorkspace.Id && existing.Title == input.Title.Trim()
                ? Results.Ok(new { existing.Id }) : Results.Conflict(new { message = "Operation ID already used." });
            var session = new InterviewSession { Id = input.Id, Title = input.Title.Trim() };
            db.Sessions.Add(session); await db.SaveChangesAsync(ct);
            return Results.Ok(new { session.Id });
        });
        app.MapGet("/api/sessions/{id:guid}", async (Guid id, TwiggleDbContext db, CancellationToken ct) =>
        {
            var session = await Load(db, id, ct);
            return session == null ? Results.NotFound() : Results.Ok(View(session));
        });
        app.MapPost("/api/sessions/{id:guid}/turns", async (Guid id, AddTurn input, TwiggleDbContext db, CancellationToken ct) =>
        {
            var s = await Load(db, id, ct); if (s == null) return Results.NotFound();
            if (input.Id == Guid.Empty || string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 20000)
                return Results.BadRequest(new { message = "Answer must contain 1–20,000 characters." });
            var existing = s.Turns.SingleOrDefault(x => x.Id == input.Id);
            if (existing != null) return existing.Text == input.Text ? Results.Ok(View(s)) : Results.Conflict(new { message = "Operation ID already used with another answer." });
            if (s.Revision != input.ExpectedRevision) return Stale();
            db.Turns.Add(new InterviewTurn { Id = input.Id, Text = input.Text, Session = s });
            s.Revision++; await db.SaveChangesAsync(ct); return Results.Ok(View(s));
        });
        app.MapPost("/api/sessions/{id:guid}/claims", async (Guid id, AddClaim input, TwiggleDbContext db, CancellationToken ct) =>
        {
            var s = await Load(db, id, ct); if (s == null) return Results.NotFound();
            if (input.Id == Guid.Empty || string.IsNullOrWhiteSpace(input.Statement) || input.Statement.Length > 4000)
                return Results.BadRequest(new { message = "Fact must contain 1–4,000 characters." });
            var existing = s.Claims.SingleOrDefault(x => x.Id == input.Id);
            if (existing != null) return existing.SourceTurnId == input.SourceTurnId && existing.Statement == input.Statement.Trim()
                ? Results.Ok(View(s)) : Results.Conflict(new { message = "Operation ID already used." });
            if (s.Revision != input.ExpectedRevision) return Stale();
            if (!s.Turns.Any(x => x.Id == input.SourceTurnId)) return Results.BadRequest(new { message = "Select a source answer from this session." });
            var claim = new ExperienceClaim { Session = s, Id = input.Id, SourceTurnId = input.SourceTurnId, Statement = input.Statement.Trim() };
            s.Revision++;
            claim.Revisions.Add(new ClaimRevision { SessionRevision = s.Revision, Statement = claim.Statement, ReviewStatus = "Pending" });
            db.Claims.Add(claim); await db.SaveChangesAsync(ct); return Results.Ok(View(s));
        });
        app.MapPut("/api/sessions/{id:guid}/claims/{claimId:guid}", async (Guid id, Guid claimId, ReviewClaim input, TwiggleDbContext db, CancellationToken ct) =>
        {
            var s = await Load(db, id, ct); if (s == null) return Results.NotFound();
            var claim = s.Claims.SingleOrDefault(x => x.Id == claimId); if (claim == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(input.Statement) || input.Statement.Length > 4000 || !new[] { "Pending", "Accepted", "Disputed" }.Contains(input.ReviewStatus))
                return Results.BadRequest(new { message = "Provide a fact and a valid review status." });
            if (claim.Statement == input.Statement.Trim() && claim.ReviewStatus == input.ReviewStatus) return Results.Ok(View(s));
            if (s.Revision != input.ExpectedRevision) return Stale();
            claim.Statement = input.Statement.Trim(); claim.ReviewStatus = input.ReviewStatus; s.Revision++;
            db.Add(new ClaimRevision { Claim = claim, SessionRevision = s.Revision, Statement = claim.Statement, ReviewStatus = claim.ReviewStatus });
            await db.SaveChangesAsync(ct); return Results.Ok(View(s));
        });
        app.MapPost("/api/sessions/{id:guid}/episode", async (Guid id, SaveEpisode input, TwiggleDbContext db, CancellationToken ct) =>
        {
            var s = await Load(db, id, ct); if (s == null) return Results.NotFound();
            if (s.Episode != null) return Results.Ok(View(s));
            if (s.Revision != input.ExpectedRevision) return Stale();
            if (!s.Claims.Any(x => x.ReviewStatus == "Accepted")) return Results.BadRequest(new { message = "Accept at least one fact before saving an experience." });
            s.Episode = new ExperienceEpisode { Id = Guid.NewGuid(), Session = s }; db.Episodes.Add(s.Episode); s.Revision++;
            await db.SaveChangesAsync(ct); return Results.Ok(View(s));
        });
        app.MapGet("/api/experiences", async (TwiggleDbContext db, CancellationToken ct) => Results.Ok(await db.Episodes
            .Where(x => x.Session.WorkspaceId == LocalWorkspace.Id).OrderByDescending(x => x.SavedAt)
            .Select(x => new { x.Id, x.SessionId, x.Session.Title, x.SavedAt, AcceptedFacts = x.Session.Claims.Count(c => c.ReviewStatus == "Accepted") }).ToListAsync(ct)));
    }
    internal static Task<InterviewSession?> Load(TwiggleDbContext db, Guid id, CancellationToken ct) => db.Sessions
        .Include(x => x.AiReplies).Include(x => x.Turns).Include(x => x.Claims).ThenInclude(x => x.Revisions).Include(x => x.Episode)
        .AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id && x.WorkspaceId == LocalWorkspace.Id, ct);
    internal static object View(InterviewSession s) => new { s.Id, s.Title, s.Revision, s.CreatedAt,
        EpisodeId = s.Episode?.Id, Turns = s.Turns.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.Text, x.CreatedAt }),
        AiReplies = s.AiReplies.OrderBy(x => x.CreatedAt).Select(x => new { x.Id, x.SourceTurnId, x.Summary, x.NextQuestion, x.QuestionReason, x.Model, x.PolicyVersion, x.SessionRevision, x.CreatedAt }),
        Claims = s.Claims.Select(x => new { x.Id, x.SourceTurnId, x.Statement, x.ReviewStatus, x.Origin, x.SourceQuote,
            Revisions = x.Revisions.OrderBy(r => r.SessionRevision).Select(r => new { r.Statement, r.ReviewStatus, r.SessionRevision, r.RecordedAt }) }) };
    static IResult Stale() => Results.Conflict(new { message = "This session changed. Reload before retrying; your text has been kept." });
}
public sealed record CreateSession(Guid Id, string Title);
public sealed record AddTurn(Guid Id, string Text, int ExpectedRevision);
public sealed record AddClaim(Guid Id, Guid SourceTurnId, string Statement, int ExpectedRevision);
public sealed record ReviewClaim(string Statement, string ReviewStatus, int ExpectedRevision);
public sealed record SaveEpisode(int ExpectedRevision);
