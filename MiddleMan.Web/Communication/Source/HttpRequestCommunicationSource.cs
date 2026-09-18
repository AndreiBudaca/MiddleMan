using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Source
{
  public class HttpRequestCommunicationSource(HttpRequest request) : ICommunicationSource
  {
    private readonly HttpRequest request = request;

    public async Task<RequestData> ReadData(CancellationToken cancellationToken = default)
    {
      return new RequestData
      {
        Metadata = new HttpRequestMetadata(request),
        Data = Adapt(request)
      };
    }

    private static async IAsyncEnumerable<DataChunk> Adapt(HttpRequest request)
    {
      while (true)
      {
        // TO DO: Consider using a buffer pool to reduce memory allocations for large requests
        var buffer = new byte[ServerCapabilities.MaxChunkSize];
        var bytesRead = await request.Body.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxChunkSize));

        if (bytesRead == 0) break;

        if (bytesRead == buffer.Length)
        {
          yield return new DataChunk(buffer, bytesRead);
        }
        else
        {
          yield return new DataChunk(buffer.Take(bytesRead).ToArray(), bytesRead);
        }
      }
    }
  }
}