using Nito.AsyncEx;

namespace MiddleMan.Web.Communication.SyncMechanisms
{
  public class SessionAsyncMonitorPool
  {
    private readonly Dictionary<Guid, AsyncMonitor?> monitors = [];
    private readonly object globalLock = new();

    public AsyncMonitor Get(Guid session)
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

    public void Discard(Guid session)
    {
      lock (globalLock)
      {
        monitors.Remove(session);
      }
    }
    
    public async Task<T> WaitToGetResource<T>(Func<Task<T>> getterFunct, Func<T, bool> resourceCondition, Guid seesion)
    {
      var resource = await getterFunct.Invoke();
      if (!resourceCondition.Invoke(resource))
      {
        var monitor = Get(seesion);
        using var leaveMonitorDisposable = await monitor.EnterAsync();

        resource = await getterFunct.Invoke();
        if (!resourceCondition.Invoke(resource))
        {
          await monitor.WaitAsync();
          resource = await getterFunct.Invoke();
        }
      }
      Discard(seesion);

      return resource;
    }

    public async Task SetResourceAndNotify(Func<Task> setterFunct, Guid seesion)
    {
      var monitor = Get(seesion);
      using var leaveMonitorDisposable = await monitor.EnterAsync();
      await setterFunct.Invoke();
      monitor.PulseAll();
      Discard(seesion);
    }
  }
}
