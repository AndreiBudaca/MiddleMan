namespace MiddleMan.Web.Resiliency
{
  public class NoBuffer(IAsyncEnumerable<byte[]> source) : IContentBuffer<byte[]>
  {
    private readonly IAsyncEnumerable<byte[]> sourceStream = source;

    public async ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);
    }

    public IAsyncEnumerable<byte[]> Read(CancellationToken cancellationToken)
    {
      return sourceStream;
    }
  }
}