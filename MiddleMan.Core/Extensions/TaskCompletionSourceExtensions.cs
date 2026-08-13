namespace MiddleMan.Core.Extensions
{
  public static class TaskCompletionSourceExtensions
  {
    public static CancellationTokenSource SetTimeout<T>(this TaskCompletionSource<T> tcs, int? timeout = null, CancellationToken cancellationToken = default)
    {
      var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(timeout ?? ServerCapabilities.GlobalTimeoutSeconds));

      cts.Token.Register(() =>
      {
        _ = tcs.TrySetCanceled();
      });

      return cts;
    }
  }
}