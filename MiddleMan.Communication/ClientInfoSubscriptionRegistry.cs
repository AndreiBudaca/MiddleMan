using System.Collections.Concurrent;

namespace MiddleMan.Communication
{
  public static class ClientInfoSubscriptionRegistry
  {
    private static readonly ConcurrentDictionary<string, IAsyncDisposable> _handles = new();
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public static bool Contains(string key) => _handles.ContainsKey(key);

    public static async Task GetOrAddAsync(string key, Func<Task<IAsyncDisposable>> factory)
    {
      if (_handles.ContainsKey(key)) return;

      await _gate.WaitAsync();
      try
      {
        if (_handles.ContainsKey(key)) return;

        var handle = await factory();
        if (!_handles.TryAdd(key, handle))
        {
          await handle.DisposeAsync();
        }
      }
      finally
      {
        _gate.Release();
      }
    }

    public static async Task RemoveAndDisposeAsync(string key)
    {
      if (_handles.TryRemove(key, out var handle))
      {
        try { await handle.DisposeAsync(); } catch { }
      }
    }

    public static async Task DisposeAllAsync()
    {
      foreach (var key in _handles.Keys)
      {
        await RemoveAndDisposeAsync(key);
      }
    }
  }
}
