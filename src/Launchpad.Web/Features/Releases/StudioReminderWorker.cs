namespace Launchpad.Web.Features.Releases;

public sealed class StudioReminderWorker(IServiceScopeFactory scopeFactory, ILogger<StudioReminderWorker> logger) : BackgroundService {
    private readonly IServiceScopeFactory scopeFactory = scopeFactory;
    private readonly ILogger<StudioReminderWorker> logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await CreateReminderAsync(stoppingToken).ConfigureAwait(false);

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            await CreateReminderAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CreateReminderAsync(CancellationToken cancellationToken) {
        try {
            using IServiceScope scope = scopeFactory.CreateScope();
            ReleaseWorkflowService workflow = scope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
            await workflow.CreateStaleBugRemindersAsync(cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
        } catch (Exception ex) {
            logger.LogWarning(ex, "Could not create studio reminder");
        }
    }
}
