namespace MiddleMan.Web.Resiliency
{
  public interface IContentBuffer : IAsyncDisposable
  {
    public IAsyncEnumerable<byte[]> Read(CancellationToken cancellationToken);
  }
}