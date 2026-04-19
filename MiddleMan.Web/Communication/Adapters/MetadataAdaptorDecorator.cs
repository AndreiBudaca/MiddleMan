using MiddleMan.Communication.Adapters;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Adapters
{
  public class MetadataAdaptorDecorator(IAsyncEnumerable<byte[]> innerAdapter, HttpRequestMetadata metadata, bool sendMetadata = false) : IDataWriterAdaptor
  {
    private readonly IAsyncEnumerable<byte[]> innerAdapter = innerAdapter;
    private readonly HttpRequestMetadata? metadata = sendMetadata ? metadata : null;

    public async IAsyncEnumerable<byte[]> Adapt()
    {
      yield return metadata?.SerializeJson() ?? BitConverter.GetBytes(0);

      await foreach (var chunk in innerAdapter)
      {
        yield return chunk;
      }
    }
  }
}