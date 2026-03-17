using Nito.AsyncEx;

namespace MiddleMan.Communication.SyncMechanisms
{
  public class AsyncResourceMonitor<T> where T : notnull
  {
    private readonly Dictionary<T, AsyncMonitor?> monitors = [];
    private readonly object globalLock = new();

    private AsyncMonitor Get(T session)
    {
      _ = monitors.TryGetValue(session, out var result);
      if (result != null) return result;

      lock (globalLock)
      {
        _ = monitors.TryGetValue(session, out result);
        if (result != null) return result;

        result = new AsyncMonitor();
        monitors.Add(session, result);
      }

      return result;
    }

    private void Discard(T session)
    {
      lock (globalLock)
      {
        monitors.Remove(session);
      }
    }

    public async Task<K> WaitToGetResource<K>(Func<Task<K>> getterFunc, Func<K, bool> resourceCondition, T session, CancellationToken cancellationToken = default)
    {
      try
      {
        var resource = await getterFunc.Invoke();
        if (!resourceCondition.Invoke(resource))
        {
          var monitor = Get(session);
          using var leaveMonitorDisposable = await monitor.EnterAsync(cancellationToken);

          resource = await getterFunc.Invoke();
          if (!resourceCondition.Invoke(resource))
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
      var monitor = Get(session);
      using var leaveMonitorDisposable = await monitor.EnterAsync();
      await setterFunc.Invoke();
      monitor.PulseAll();
      Discard(session);
    }
  }
}
