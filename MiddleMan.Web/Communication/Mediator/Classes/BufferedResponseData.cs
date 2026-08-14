using MiddleMan.Core;
using MiddleMan.Web.Resiliency;

namespace MiddleMan.Web.Communication.Mediator.Classes
{
  public class BufferedResponseData(ResponseData? source) : IAsyncDisposable
  {
    private readonly ResponseData? source = source;
    private readonly HybridBuffer? buffer = source?.Data != null ? 
      new HybridBuffer(source.Data, ServerCapabilities.MaxMemoryBufferSize) : 
      null;

    public ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);
      
      if (buffer != null)
      {
        return buffer.DisposeAsync();
      }

      return ValueTask.CompletedTask;
    }

    public async Task<ResponseData?> Read(CancellationToken cancellationToken)
    {
      if (source == null || buffer == null) return source;

      await EnsureDataBuffered(cancellationToken);
      
      return new ResponseData
      {
        Metadata = source.Metadata,
        Data = buffer.Read(cancellationToken)
      };
    }

    private async Task EnsureDataBuffered(CancellationToken cancellationToken)
    {
      if (buffer == null) return;

      await foreach (var _ in buffer.Read(cancellationToken)) { }
    }
  }
}