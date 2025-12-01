using Nito.AsyncEx;

namespace MiddleMan.Web.Communication.SyncMechanisms
{
  public class AsyncResourceMonitor
  {
    private readonly Dictionary<Guid, AsyncMonitor?> monitors = [];
    private readonly object globalLock = new();

    private AsyncMonitor Get(Guid session)
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

    private void Discard(Guid session)
    {
      lock (globalLock)
      {
        monitors.Remove(session);
      }
    }
    
    public async Task<T> WaitToGetResource<T>(Func<Task<T>> getterFunc, Func<T, bool> resourceCondition, Guid session)
    {
      var resource = await getterFunc.Invoke();
      if (!resourceCondition.Invoke(resource))
      {
        var monitor = Get(session);
        using var leaveMonitorDisposable = await monitor.EnterAsync();

        resource = await getterFunc.Invoke();
        if (!resourceCondition.Invoke(resource))
        {
          await monitor.WaitAsync();
          resource = await getterFunc.Invoke();
        }
      }
      Discard(session);

      return resource;
    }

    public async Task SetResourceAndNotify(Func<Task> setterFunc, Guid session)
    {
      var monitor = Get(session);
      using var leaveMonitorDisposable = await monitor.EnterAsync();
      await setterFunc.Invoke();
      monitor.PulseAll();
      Discard(session);
    }
  }
}
