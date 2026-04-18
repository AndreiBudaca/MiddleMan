namespace MiddleMan.Web.Resiliency
{
  public interface IContentBuffer<T> : IAsyncDisposable
  {
    public IAsyncEnumerable<byte[]> Read(CancellationToken cancellationToken);
  }
}