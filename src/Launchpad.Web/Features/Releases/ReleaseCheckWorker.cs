using System.Globalization;

namespace Launchpad.Web.Features.Releases;

public sealed partial class ReleaseCheckWorker(
    IServiceScopeFactory scopeFactory,
    ReleaseCheckSignal signal,
    ILogger<ReleaseCheckWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ReleaseCheckSignal _signal = signal;
    private readonly ILogger<ReleaseCheckWorker> _logger = logger;
    private readonly string _workerId = string.Create(CultureInfo.InvariantCulture, $"worker-{Environment.MachineName}-{Environment.ProcessId}");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(_workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (await ProcessOneAsync(stoppingToken).ConfigureAwait(false))
            {
            }

            await _signal.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Release check worker started as {WorkerId}")]
    private partial void LogWorkerStarted(string workerId);

    private async Task<bool> ProcessOneAsync(CancellationToken cancellationToken)
    {
        ReleaseCheckClaim? claim;
        AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            ReleaseWorkflowService workflow = scope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
            claim = await workflow.ClaimNextQueuedCheckAsync(_workerId, cancellationToken).ConfigureAwait(false);
        }

        if (claim is null)
        {
            return false;
        }

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

        AsyncServiceScope completeScope = _scopeFactory.CreateAsyncScope();
        await using (completeScope.ConfigureAwait(false))
        {
            ReleaseWorkflowService completeWorkflow = completeScope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
            await completeWorkflow.CompleteCheckAsync(claim.Value.RunId, passed: true, completeWorkflow.BuildRunLog(claim.Value.Kind), cancellationToken).ConfigureAwait(false);
        }

        return true;
    }
}
