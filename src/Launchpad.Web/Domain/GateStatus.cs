namespace Launchpad.Web.Domain;

public enum GateStatus {
    Pending = 0,
    Queued = 1,
    Running = 2,
    Passed = 3,
    Failed = 4,
}
