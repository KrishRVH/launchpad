namespace Launchpad.Web.Features.Notifications;

public sealed class LaunchpadNotifier {
    public event EventHandler? Changed;

    public ValueTask PublishAsync() {
        Changed?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }
}
