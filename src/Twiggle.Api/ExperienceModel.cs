namespace Twiggle.Api;

public sealed class InterviewSession
{
    public Guid Id { get; set; }
    public string WorkspaceId { get; set; } = LocalWorkspace.Id;
    public string Title { get; set; } = "";
    public int Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<InterviewTurn> Turns { get; set; } = [];
    public List<ExperienceClaim> Claims { get; set; } = [];
    public ExperienceEpisode? Episode { get; set; }
}
public sealed class InterviewTurn
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession Session { get; set; } = null!;
    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class ExperienceClaim
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession Session { get; set; } = null!;
    public Guid SourceTurnId { get; set; }
    public InterviewTurn SourceTurn { get; set; } = null!;
    public string Statement { get; set; } = "";
    public string ReviewStatus { get; set; } = "Pending";
    public string Origin { get; set; } = "UserAuthored";
    public List<ClaimRevision> Revisions { get; set; } = [];
}
public sealed class ClaimRevision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public ExperienceClaim Claim { get; set; } = null!;
    public int SessionRevision { get; set; }
    public string Statement { get; set; } = "";
    public string ReviewStatus { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class ExperienceEpisode
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InterviewSession Session { get; set; } = null!;
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
}
public static class LocalWorkspace { public const string Id = "local-development"; }
