namespace Launchpad.Web.Domain;

public sealed class ReleaseGate {
    public Guid Id { get; set; }
    public Guid GameReleaseId { get; set; }
    public GameRelease Release { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
    public GateStatus Status { get; set; } = GateStatus.Pending;

    public List<ReleaseCheckRun> CheckRuns { get; set; } = [];
}
