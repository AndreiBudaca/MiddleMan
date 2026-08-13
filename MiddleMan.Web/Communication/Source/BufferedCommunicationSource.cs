using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Resiliency;

namespace MiddleMan.Web.Communication.Source
{
  public class BufferedCommunicationSource(ICommunicationSource wrappedSource) : ICommunicationSource, IAsyncDisposable
  {
    private readonly ICommunicationSource wrappedSource = wrappedSource;
    private HybridBuffer? buffer = null;
    private HttpRequestMetadata? requestMetadata = null;

    public async Task<RequestData> ReadData(CancellationToken cancellationToken = default)
    {
      if (!ServerCapabilities.FaultToleranceEnabled) return await wrappedSource.ReadData(cancellationToken);

      var buffer = await GetBuffer(cancellationToken);

      return new RequestData
      {
        Metadata = requestMetadata,
        Data = buffer.Read(cancellationToken)
      };
    }

    public ValueTask DisposeAsync()
    {
      GC.SuppressFinalize(this);

      if (buffer != null)
      {
        return buffer.DisposeAsync();
      }

      return ValueTask.CompletedTask;
    }

    private async Task<HybridBuffer> GetBuffer(CancellationToken cancellationToken = default)
    {
      if (buffer != null) return buffer;

      var sourceData = await wrappedSource.ReadData(cancellationToken);

      requestMetadata = sourceData.Metadata;
      buffer = new HybridBuffer(sourceData.Data, ServerCapabilities.MaxMemoryBufferSize);

      return buffer;
    }
  }
}