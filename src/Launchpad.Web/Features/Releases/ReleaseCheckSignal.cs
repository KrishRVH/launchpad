namespace Launchpad.Web.Features.Releases;

public sealed class ReleaseCheckSignal : IDisposable {
    private readonly SemaphoreSlim signal = new(0);

    public ValueTask PulseAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        signal.Release();
        return ValueTask.CompletedTask;
    }

    public async Task WaitAsync(TimeSpan pollInterval, CancellationToken cancellationToken) {
        _ = await signal.WaitAsync(pollInterval, cancellationToken).ConfigureAwait(false);
        while (signal.Wait(millisecondsTimeout: 0, cancellationToken)) {
        }
    }

    public void Dispose() => signal.Dispose();
}
