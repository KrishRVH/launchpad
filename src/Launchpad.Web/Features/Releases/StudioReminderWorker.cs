namespace Launchpad.Web.Features.Releases;

public sealed partial class StudioReminderWorker(IServiceScopeFactory scopeFactory, ILogger<StudioReminderWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<StudioReminderWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CreateReminderAsync(stoppingToken).ConfigureAwait(false);

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await CreateReminderAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not create studio reminder")]
    private partial void LogReminderFailure(Exception exception);

    private async Task CreateReminderAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ReleaseWorkflowService workflow = scope.ServiceProvider.GetRequiredService<ReleaseWorkflowService>();
            await workflow.CreateStaleBugRemindersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogReminderFailure(ex);
        }
    }
}
