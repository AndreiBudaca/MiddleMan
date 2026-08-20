using Nito.AsyncEx;

namespace MiddleMan.Communication.SyncMechanisms
{
  public class TokenCounter
  {
    private int count = 0;
    private readonly AsyncMonitor monitor = new();

    public void Increment()
    {
      using (_ = monitor.Enter())
      {
        count++;
        monitor.Pulse();
      }
    }

    public async Task Decrement(CancellationToken cancellationToken)
    {
      using (_ = monitor.Enter(cancellationToken))
      {
        if (count <= 0) await monitor.WaitAsync(cancellationToken);
        count--;
      }
    }
  }
}