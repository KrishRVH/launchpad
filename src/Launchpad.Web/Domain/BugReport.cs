namespace Launchpad.Web.Domain;

public sealed class BugReport {
    public Guid Id { get; set; }
    public Guid GameReleaseId { get; set; }
    public GameRelease Release { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public BugSeverity Severity { get; set; } = BugSeverity.Medium;
    public BugStatus Status { get; set; } = BugStatus.New;
    public string? OwnerUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
