using Launchpad.Web.Domain;

namespace Launchpad.Tests;

[TestClass]
public sealed class ReleasePolicyTests
{
    [TestMethod]
    public void CanApproveWhenEveryRequiredGatePassed()
    {
        GameRelease release = new()
        {
            Gates =
            {
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "QA", Status = GateStatus.Passed },
                new ReleaseGate { Name = "Security", Status = GateStatus.Passed },
            },
        };

        Assert.IsTrue(ReleasePolicy.CanApprove(release));
    }

    [TestMethod]
    public void CannotApproveWhenARequiredGateIsPending()
    {
        GameRelease release = new()
        {
            Gates =
            {
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "QA", Status = GateStatus.Pending },
            },
        };

        Assert.IsFalse(ReleasePolicy.CanApprove(release));
    }

    [TestMethod]
    public void CannotApproveReleaseWithoutRequiredGates()
    {
        GameRelease release = new()
        {
            Gates =
            {
                new ReleaseGate { Name = "Nice to have", IsRequired = false, Status = GateStatus.Passed },
            },
        };

        Assert.IsFalse(ReleasePolicy.CanApprove(release));
    }

    [TestMethod]
    public void OptionalFailedGateDoesNotBlockApproval()
    {
        GameRelease release = new()
        {
            Gates =
            {
                new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
                new ReleaseGate { Name = "Optional telemetry", IsRequired = false, Status = GateStatus.Failed },
            },
        };

        Assert.IsTrue(ReleasePolicy.CanApprove(release));
    }

    [TestMethod]
    public void FailedGateBlocksRelease()
    {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
            new ReleaseGate { Name = "QA", Status = GateStatus.Failed },
        ]);

        Assert.AreEqual(ReleaseStatus.Blocked, status);
    }

    [TestMethod]
    [DataRow(GateStatus.Pending, ReleaseStatus.Draft)]
    [DataRow(GateStatus.Queued, ReleaseStatus.Checking)]
    [DataRow(GateStatus.Running, ReleaseStatus.Checking)]
    public void IncompleteRequiredGateDeterminesReleaseStatus(GateStatus gateStatus, ReleaseStatus expectedStatus)
    {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = gateStatus },
        ]);

        Assert.AreEqual(expectedStatus, status);
    }

    [TestMethod]
    public void PassedRequiredGatesMakeReleaseReady()
    {
        ReleaseStatus status = ReleasePolicy.StatusForGates([
            new ReleaseGate { Name = "Build", Status = GateStatus.Passed },
            new ReleaseGate { Name = "Optional telemetry", IsRequired = false, Status = GateStatus.Failed },
        ]);

        Assert.AreEqual(ReleaseStatus.Ready, status);
    }
}
