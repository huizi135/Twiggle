using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Twiggle.Api;

public static class AiInterviewEndpoints
{
    public static bool HasValidSources(InterviewDraft draft, InterviewSession session) => draft.Facts.All(fact =>
        fact != null && session.Turns.Any(source => source.Id == fact.SourceTurnId &&
            !string.IsNullOrWhiteSpace(fact.Quote) && source.Text.Contains(fact.Quote, StringComparison.Ordinal)) &&
        !string.IsNullOrWhiteSpace(fact.Statement) && fact.Statement.Length <= 4000);
    public static void MapAiInterview(this WebApplication app)
    {
        app.MapGet("/api/ai/status", (AiInterviewer ai) => Results.Ok(new {
            configured = ai.Configured, provider = "OpenAI", model = ai.Model,
            destination = "https://api.openai.com/v1/responses", policyVersion = AiInterviewer.PolicyVersion
        }));
        app.MapPost("/api/sessions/{id:guid}/follow-up", async (Guid id, FollowUp input,
            TwiggleDbContext db, AiInterviewer ai, InterviewLocks locks, CancellationToken ct) =>
        {
            if (!input.AllowExternalProcessing) return Results.BadRequest(new { message = "Enable AI for this session before sending interview context to OpenAI." });
            var gate = locks.For(id);
            await gate.WaitAsync(ct);
            try
            {
                var s = await ExperienceEndpoints.Load(db, id, ct); if (s == null) return Results.NotFound();
                var turn = s.Turns.SingleOrDefault(x => x.Id == input.TurnId);
                if (turn == null) return Results.BadRequest(new { message = "The original answer must be saved first." });
                // Successful retries reuse the persisted response; they do not repeat provider work.
                if (s.AiReplies.Any(x => x.SourceTurnId == turn.Id)) return Results.Ok(ExperienceEndpoints.View(s));
                if (s.Revision != input.ExpectedRevision) return Results.Conflict(new { message = "Session changed. Reload before requesting a follow-up." });
                if (!ai.Configured) return Results.Problem(statusCode: 503, title: "AI setup required", detail: "Run scripts/configure-ai.ps1 and restart the API. Your answer is already saved.");
                if (s.Turns.OrderByDescending(x => x.CreatedAt).First().Id != turn.Id)
                    return Results.Conflict(new { message = "Request a follow-up for the latest saved answer." });
                if (s.Turns.Sum(x => x.Text.Length) + s.Claims.Sum(x => x.Statement.Length) + s.AiReplies.Sum(x => x.Summary.Length + x.NextQuestion.Length) > 100000)
                    return Results.BadRequest(new { message = "This interview is too long for this prototype. Start a focused episode; your existing answers remain saved." });
                var context = new {
                    s.Title, latestAnswerId = turn.Id,
                    answers = s.Turns.OrderBy(x => x.CreatedAt).Select(x => new { sourceTurnId = x.Id, x.Text }),
                    previousResponses = s.AiReplies.OrderBy(x => x.CreatedAt).Select(x => new { x.SourceTurnId, x.Summary, x.NextQuestion }),
                    claims = s.Claims.Select(x => new { x.Statement, x.ReviewStatus, x.SourceTurnId })
                };
                InterviewDraft draft;
                try { draft = await ai.GenerateAsync(context, ct); }
                catch (AiProviderException ex) { return Results.Problem(statusCode: 502, title: "AI follow-up unavailable", detail: ex.Message); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Results.Problem(statusCode: 504, title: "AI timed out", detail: "Your answer is saved. Retry the follow-up."); }
                catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
                { return Results.Problem(statusCode: 502, title: "AI response unavailable", detail: "The provider response could not be read. Your answer is saved; retry later."); }
                // All candidates must point to an existing source and a verbatim quote, before any write.
                if (!HasValidSources(draft, s))
                    return Results.Problem(statusCode: 502, title: "AI source validation failed", detail: "An AI candidate had an invalid source. No AI facts were saved; your original answer is safe. Retry the follow-up.");
                s.Revision++;
                foreach (var fact in draft.Facts.DistinctBy(x => (x.SourceTurnId, x.Statement.Trim())))
                {
                    if (s.Claims.Any(x => x.SourceTurnId == fact.SourceTurnId && string.Equals(x.Statement, fact.Statement.Trim(), StringComparison.OrdinalIgnoreCase))) continue;
                    var claim = new ExperienceClaim { Id = Guid.NewGuid(), Session = s, SourceTurnId = fact.SourceTurnId,
                        SourceQuote = fact.Quote, Statement = fact.Statement.Trim(), Origin = "AIExtracted", ReviewStatus = "Pending" };
                    claim.Revisions.Add(new ClaimRevision { SessionRevision = s.Revision, Statement = claim.Statement, ReviewStatus = "Pending" });
                    db.Claims.Add(claim);
                }
                db.AiReplies.Add(new AiReply { Id = Guid.NewGuid(), Session = s, SourceTurnId = turn.Id, Summary = draft.Summary,
                    NextQuestion = draft.NextQuestion, QuestionReason = draft.QuestionReason, Model = ai.Model,
                    PolicyVersion = AiInterviewer.PolicyVersion, SessionRevision = s.Revision, ExternalProcessingAllowedAt = DateTimeOffset.UtcNow });
                await db.SaveChangesAsync(ct);
                return Results.Ok(ExperienceEndpoints.View(s));
            }
            finally { gate.Release(); }
        });
    }
}
public sealed record FollowUp(Guid TurnId, int ExpectedRevision, bool AllowExternalProcessing);
