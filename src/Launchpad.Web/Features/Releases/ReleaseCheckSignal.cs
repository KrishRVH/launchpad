namespace Launchpad.Web.Features.Releases;

public sealed class ReleaseCheckSignal : IDisposable
{
    private readonly SemaphoreSlim _signal = new(0);

    public ValueTask PulseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _signal.Release();
        return ValueTask.CompletedTask;
    }

    public async Task WaitAsync(TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        _ = await _signal.WaitAsync(pollInterval, cancellationToken).ConfigureAwait(false);
        while (_signal.Wait(millisecondsTimeout: 0, cancellationToken))
        {
        }
    }

    public void Dispose() => _signal.Dispose();
}
