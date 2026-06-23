namespace Launchpad.Web.Domain;

public sealed class GameRelease {
    public Guid Id { get; set; }
    public Guid GameProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public string Version { get; set; } = "";
    public string Codename { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateOnly TargetDate { get; set; }
    public ReleaseStatus Status { get; set; } = ReleaseStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? BlockedReason { get; set; }

    public List<ReleaseGate> Gates { get; set; } = [];
    public List<BugReport> Bugs { get; set; } = [];
}
