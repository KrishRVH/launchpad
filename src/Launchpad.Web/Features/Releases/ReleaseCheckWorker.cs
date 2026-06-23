using System.Globalization;

namespace Launchpad.Web.Features.Releases;

public sealed class ReleaseCheckWorker(
    IServiceScopeFactory scopeFactory,
    ReleaseCheckSignal signal,
    ILogger<ReleaseCheckWorker> logger) : BackgroundService {
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly ReleaseCheckSignal signal = signal;
    private readonly ILogger<ReleaseCheckWorker> logger = logger;
    private readonly string workerId = string.Create(CultureInfo.InvariantCulture, $"worker-{Environment.MachineName}-{Environment.ProcessId}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        logger.LogInformation("Release check worker started as {WorkerId}", workerId);

        while (!stoppingToken.IsCancellationRequested) {
            while (await ProcessOneAsync(stoppingToken).ConfigureAwait(false)) {
            }

            await signal.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken) {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ReleaseWorkflowService workflow = scope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
        ReleaseCheckClaim? claim = await workflow.ClaimNextQueuedCheckAsync(workerId, cancellationToken).ConfigureAwait(false);

        if (claim is null) {
            return false;
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

        await using AsyncServiceScope completeScope = scopeFactory.CreateAsyncScope();
        ReleaseWorkflowService completeWorkflow = completeScope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
        await completeWorkflow.CompleteCheckAsync(claim.Value.RunId, passed: true, completeWorkflow.BuildRunLog(claim.Value.Kind), cancellationToken).ConfigureAwait(false);
        return true;
    }
}
