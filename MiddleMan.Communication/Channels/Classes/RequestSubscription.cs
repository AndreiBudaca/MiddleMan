namespace MiddleMan.Communication.Channels.Classes
{
  internal sealed class RequestSubscription(CancellationTokenSource cts, Task loop) : IAsyncDisposable
  {
    private int _disposed;

    public async ValueTask DisposeAsync()
    {
      if (Interlocked.Exchange(ref _disposed, 1) != 0)
      {
        try { await loop; } catch { }
        return;
      }

      try { await cts.CancelAsync(); } catch { }
      try { await loop; } catch { }
      cts.Dispose();
    }
  }
}