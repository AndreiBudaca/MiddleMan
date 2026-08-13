namespace MiddleMan.Web.Resiliency
{
  public interface IContentBuffer<T> : IAsyncDisposable
  {
    public IAsyncEnumerable<T> Read(CancellationToken cancellationToken);
  }
}