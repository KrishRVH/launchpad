namespace Launchpad.Web.Domain;

public static class ReleasePolicy {
    public static bool CanApprove(GameRelease release) {
        ReleaseGate[] required = [.. release.Gates.Where(gate => gate.IsRequired)];
        return required.Length > 0 && required.All(gate => gate.Status == GateStatus.Passed);
    }

    public static ReleaseStatus StatusForGates(IEnumerable<ReleaseGate> gates) {
        ReleaseGate[] required = [.. gates.Where(gate => gate.IsRequired)];
        if (required.Any(gate => gate.Status == GateStatus.Failed)) {
            return ReleaseStatus.Blocked;
        }

        return required.Length > 0 && required.All(gate => gate.Status == GateStatus.Passed)
            ? ReleaseStatus.Ready
            : required.Any(gate => gate.Status is GateStatus.Queued or GateStatus.Running) ? ReleaseStatus.Checking : ReleaseStatus.Draft;
    }
}
