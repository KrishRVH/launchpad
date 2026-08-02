using System.Globalization;
using Launchpad.Web.Data;
using Launchpad.Web.Domain;
using Launchpad.Web.Features.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Launchpad.Web.Features.Releases;

public sealed partial class ReleaseWorkflowService(
    ApplicationDbContext db,
    ReleaseCheckSignal signal,
    LaunchpadNotifier notifier,
    TimeProvider timeProvider,
    ILogger<ReleaseWorkflowService> logger)
{
    private readonly ApplicationDbContext _db = db;
    private readonly ReleaseCheckSignal _signal = signal;
    private readonly LaunchpadNotifier _notifier = notifier;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ReleaseWorkflowService> _logger = logger;

    public async Task<ReleaseWorkflowResult> StartChecksAsync(Guid releaseId, string userId, CancellationToken cancellationToken = default)
    {
        GameRelease? release = await _db.GameReleases
            .Include(x => x.Gates.OrderBy(gate => gate.SortOrder))
            .SingleOrDefaultAsync(x => x.Id == releaseId, cancellationToken).ConfigureAwait(false);

        if (release is null)
        {
            return ReleaseWorkflowResult.Fail("Release not found.");
        }

        if (release.Status == ReleaseStatus.Approved)
        {
            return ReleaseWorkflowResult.Fail("Approved releases cannot be checked again.");
        }

        release.Status = ReleaseStatus.Checking;
        release.BlockedReason = null;

        DateTimeOffset now = _timeProvider.GetUtcNow();
        List<Guid> runIds = [];
        foreach (ReleaseGate gate in release.Gates)
        {
            gate.Status = GateStatus.Queued;
            ReleaseCheckRun run = new()
            {
                Id = Guid.NewGuid(),
                ReleaseGateId = gate.Id,
                Kind = gate.Name,
                Status = GateStatus.Queued,
                RequestedByUserId = userId,
                QueuedAt = now,
                Log = $"Queued {gate.Name} check.",
            };
            _db.ReleaseCheckRuns.Add(run);
            runIds.Add(run.Id);
        }

        _db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = userId,
            Action = "release.checks_queued",
            EntityType = nameof(GameRelease),
            EntityId = release.Id.ToString(),
            Detail = $"Queued {runIds.Count} release checks for {release.Version}.",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (runIds.Count > 0)
        {
            await _signal.PulseAsync(cancellationToken).ConfigureAwait(false);
        }

        await _notifier.PublishAsync().ConfigureAwait(false);
        return ReleaseWorkflowResult.Ok("Release checks queued.");
    }

    public async Task<ReleaseWorkflowResult> ApproveAsync(Guid releaseId, string userId, CancellationToken cancellationToken = default)
    {
        GameRelease? release = await _db.GameReleases
            .Include(x => x.Gates)
            .SingleOrDefaultAsync(x => x.Id == releaseId, cancellationToken).ConfigureAwait(false);

        if (release is null)
        {
            return ReleaseWorkflowResult.Fail("Release not found.");
        }

        if (!ReleasePolicy.CanApprove(release))
        {
            return ReleaseWorkflowResult.Fail("All required gates must pass before launch approval.");
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        release.Status = ReleaseStatus.Approved;
        release.ApprovedAt = now;
        release.ApprovedByUserId = userId;
        release.BlockedReason = null;

        _db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = userId,
            Action = "release.approved",
            EntityType = nameof(GameRelease),
            EntityId = release.Id.ToString(),
            Detail = $"{release.Version} approved for launch.",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
        return ReleaseWorkflowResult.Ok("Release approved for launch.");
    }

    public async Task<ReleaseWorkflowResult> BlockAsync(Guid releaseId, string userId, string reason, CancellationToken cancellationToken = default)
    {
        GameRelease? release = await _db.GameReleases.SingleOrDefaultAsync(x => x.Id == releaseId, cancellationToken).ConfigureAwait(false);
        if (release is null)
        {
            return ReleaseWorkflowResult.Fail("Release not found.");
        }

        release.Status = ReleaseStatus.Blocked;
        release.BlockedReason = string.IsNullOrWhiteSpace(reason) ? "Blocked by release team." : reason.Trim();

        DateTimeOffset now = _timeProvider.GetUtcNow();
        _db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = userId,
            Action = "release.blocked",
            EntityType = nameof(GameRelease),
            EntityId = release.Id.ToString(),
            Detail = release.BlockedReason,
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
        return ReleaseWorkflowResult.Ok("Release blocked.");
    }

    public async Task<ReleaseCheckClaim?> ClaimNextQueuedCheckAsync(string workerId, CancellationToken cancellationToken)
    {
        ReleaseCheckRun? run = await _db.ReleaseCheckRuns
            .Include(x => x.Gate)
            .Where(x => x.Status == GateStatus.Queued && x.AttemptCount < 3)
            .OrderBy(x => x.QueuedAt)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (run is null)
        {
            return null;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        run.Status = GateStatus.Running;
        run.AttemptCount++;
        run.ClaimedAt = now;
        run.StartedAt = now;
        run.ClaimedBy = workerId;
        run.Log += $"{Environment.NewLine}Claimed by {workerId}.";
        run.Gate.Status = GateStatus.Running;

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = "release_check.running",
            EntityType = nameof(ReleaseCheckRun),
            EntityId = run.Id.ToString(),
            Detail = $"{run.Kind} check started.",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
        return new ReleaseCheckClaim(run.Id, run.Kind);
    }

    public async Task CompleteCheckAsync(Guid runId, bool passed, string log, CancellationToken cancellationToken)
    {
        ReleaseCheckRun run = await _db.ReleaseCheckRuns
            .Include(x => x.Gate)
            .ThenInclude(x => x.Release)
            .ThenInclude(x => x.Gates)
            .SingleAsync(x => x.Id == runId, cancellationToken).ConfigureAwait(false);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        run.Status = passed ? GateStatus.Passed : GateStatus.Failed;
        run.CompletedAt = now;
        run.Log += Environment.NewLine + log;
        run.FailureReason = passed ? null : "Release check failed.";
        run.Gate.Status = run.Status;

        GameRelease release = run.Gate.Release;
        if (release.Status != ReleaseStatus.Approved)
        {
            release.Status = ReleasePolicy.StatusForGates(release.Gates);
            if (release.Status == ReleaseStatus.Blocked)
            {
                release.BlockedReason = $"{run.Gate.Name} failed.";
            }
        }

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = passed ? "release_check.passed" : "release_check.failed",
            EntityType = nameof(ReleaseCheckRun),
            EntityId = run.Id.ToString(),
            Detail = $"{run.Kind} check {(passed ? "passed" : "failed")}.",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
    }

    public async Task<ReleaseWorkflowResult> RecordExternalCheckResultAsync(Guid runId, bool passed, string log, CancellationToken cancellationToken)
    {
        bool exists = await _db.ReleaseCheckRuns.AnyAsync(x => x.Id == runId, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return ReleaseWorkflowResult.Fail("Check run not found.");
        }

        await CompleteCheckAsync(runId, passed, string.IsNullOrWhiteSpace(log) ? "External build agent submitted result." : log, cancellationToken).ConfigureAwait(false);
        return ReleaseWorkflowResult.Ok("External check result accepted.");
    }

    public async Task<ReleaseWorkflowResult> SubmitFeedbackAsync(
        string projectCodeName,
        string? releaseVersion,
        string testerAlias,
        FeedbackSentiment sentiment,
        string body,
        string source,
        CancellationToken cancellationToken)
    {
        GameProject? project = await _db.GameProjects
            .Include(x => x.Releases)
            .SingleOrDefaultAsync(x => x.CodeName == projectCodeName, cancellationToken).ConfigureAwait(false);

        if (project is null)
        {
            return ReleaseWorkflowResult.Fail("Project not found.");
        }

        GameRelease? release = string.IsNullOrWhiteSpace(releaseVersion)
            ? null
            : project.Releases.SingleOrDefault(x => string.Equals(x.Version, releaseVersion, StringComparison.Ordinal));

        DateTimeOffset now = _timeProvider.GetUtcNow();
        _db.PlaytestFeedback.Add(new PlaytestFeedback
        {
            GameProjectId = project.Id,
            GameReleaseId = release?.Id,
            TesterAlias = string.IsNullOrWhiteSpace(testerAlias) ? "Anonymous tester" : testerAlias.Trim(),
            Sentiment = sentiment,
            Body = body.Trim(),
            Source = source,
            CreatedAt = now,
        });

        _db.AuditEvents.Add(new AuditEvent
        {
            Action = "playtest_feedback.ingested",
            EntityType = nameof(PlaytestFeedback),
            EntityId = project.Id.ToString(),
            Detail = $"Feedback ingested from {source}.",
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
        return ReleaseWorkflowResult.Ok("Feedback accepted.");
    }

    public async Task CreateStaleBugRemindersAsync(CancellationToken cancellationToken)
    {
        int staleCount = await _db.BugReports
            .CountAsync(x => x.Status != BugStatus.Fixed && x.Severity >= BugSeverity.High, cancellationToken).ConfigureAwait(false);

        if (staleCount == 0)
        {
            return;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();
        DateTimeOffset since = now.AddHours(-12);
        bool alreadySent = await _db.TeamNotifications
            .AnyAsync(x => x.Title == "High-severity bugs need review" && x.CreatedAt >= since, cancellationToken).ConfigureAwait(false);

        if (alreadySent)
        {
            return;
        }

        _db.TeamNotifications.Add(new TeamNotification
        {
            Title = "High-severity bugs need review",
            Body = string.Create(CultureInfo.InvariantCulture, $"{staleCount} high-severity bug(s) are still open before launch."),
            CreatedAt = now,
        });
        _db.AuditEvents.Add(new AuditEvent
        {
            Action = "bug.reminder_created",
            EntityType = nameof(BugReport),
            Detail = string.Create(CultureInfo.InvariantCulture, $"{staleCount} high-severity bug(s) require review."),
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _notifier.PublishAsync().ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulating {Kind} release check")]
    private partial void LogSimulatedCheck(string kind);

    public string BuildRunLog(string kind)
    {
        LogSimulatedCheck(kind);
        return kind switch
        {
            "Build" => "Build restored packages, compiled game server, and packed artifacts.",
            "QA" => "QA smoke suite covered tutorial, combat loop, save/load, and controller flow.",
            "Security" => "Security scan checked secrets, dependency advisories, and admin endpoints.",
            _ => $"{kind} check completed.",
        };
    }
}
