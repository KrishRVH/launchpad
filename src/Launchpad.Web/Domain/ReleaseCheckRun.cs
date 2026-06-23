namespace Launchpad.Web.Domain;

public sealed class ReleaseCheckRun {
    public Guid Id { get; set; }
    public Guid ReleaseGateId { get; set; }
    public ReleaseGate Gate { get; set; } = null!;
    public string Kind { get; set; } = "";
    public GateStatus Status { get; set; } = GateStatus.Queued;
    public string RequestedByUserId { get; set; } = "";
    public string Log { get; set; } = "";
    public int AttemptCount { get; set; }
    public DateTimeOffset QueuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClaimedAt { get; set; }
    public string? ClaimedBy { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
}
