using Nito.AsyncEx;

namespace MiddleMan.Communication.SyncMechanisms
{
  public class AsyncResourceMonitor<T> where T : notnull
  {
    private readonly Dictionary<T, AsyncMonitor?> monitors = [];
    private readonly object globalLock = new();

    public async Task<K> WaitToGetResource<K>(Func<Task<K>> getterFunc, Func<K, bool> resourceCondition, T session, CancellationToken cancellationToken = default)
    {
      try
      {
        var resource = await getterFunc.Invoke();
        if (!resourceCondition.Invoke(resource))
        {
          var monitor = GetOrCreate(session);
          using var leaveMonitorDisposable = await monitor.EnterAsync(cancellationToken);

          resource = await getterFunc.Invoke();
          while (!resourceCondition.Invoke(resource))
          {
            await monitor.WaitAsync(cancellationToken);
            resource = await getterFunc.Invoke();
          }
        }
        return resource;
      }
      finally
      {
        Discard(session);
      }
    }

    public async Task SetResourceAndNotify(Func<Task> setterFunc, T session)
    {
      await setterFunc.Invoke();

      var monitor = Get(session);
      if (monitor == null) return; // session discarded or not initialized yet, no need to pulse

      using var leaveMonitorDisposable = await monitor.EnterAsync();
      monitor.PulseAll();
    }

    private AsyncMonitor? Get(T session)
    {
      lock (globalLock)
      {
        _ = monitors.TryGetValue(session, out var result);
        return result;
      }
    }

    private AsyncMonitor GetOrCreate(T session)
    {
      lock (globalLock)
      {
        var exists = monitors.TryGetValue(session, out var result);
        if (exists && result != null) return result;

        result = new AsyncMonitor();
        monitors.Add(session, result);
        return result;
      }
    }

    private void Discard(T session)
    {
      lock (globalLock)
      {
        monitors.Remove(session);
      }
    }
  }
}
