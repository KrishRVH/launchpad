using Launchpad.Web.Domain;

namespace Launchpad.Tests;

public sealed class ReleasePolicyTests {
    [Fact]
    public void CanApproveWhenEveryRequiredGatePassed() {
        GameRelease release = new() {
            Gates = [
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "QA", Status = GateStatus.Passed },
                new ReleaseGate { Name = "Security", Status = GateStatus.Passed },
            ],
        };

        Assert.True(ReleasePolicy.CanApprove(release));
    }

    [Fact]
    public void CannotApproveWhenARequiredGateIsPending() {
        GameRelease release = new() {
            Gates = [
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "QA", Status = GateStatus.Pending },
            ],
        };

        Assert.False(ReleasePolicy.CanApprove(release));
    }

    [Fact]
    public void CannotApproveReleaseWithoutRequiredGates() {
        GameRelease release = new() {
            Gates = [
                new ReleaseGate { Name = "Nice to have", IsRequired = false, Status = GateStatus.Passed },
            ],
        };

        Assert.False(ReleasePolicy.CanApprove(release));
    }

    [Fact]
    public void OptionalFailedGateDoesNotBlockApproval() {
        GameRelease release = new() {
            Gates = [
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "Optional telemetry", IsRequired = false, Status = GateStatus.Failed },
            ],
        };

        Assert.True(ReleasePolicy.CanApprove(release));
    }

    [Fact]
    public void FailedGateBlocksRelease() {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
            new ReleaseGate { Name = "QA", Status = GateStatus.Failed },
        ]);

        Assert.Equal(ReleaseStatus.Blocked, status);
    }

    [Fact]
    public void RunningGateKeepsReleaseChecking() {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = GateStatus.Running },
            new ReleaseGate { Name = "QA", Status = GateStatus.Pending },
        ]);

        Assert.Equal(ReleaseStatus.Checking, status);
    }

    [Fact]
    public void PassedRequiredGatesMakeReleaseReady() {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
            new ReleaseGate { Name = "Optional telemetry", IsRequired = false, Status = GateStatus.Failed },
        ]);

        Assert.Equal(ReleaseStatus.Ready, status);
    }
}
