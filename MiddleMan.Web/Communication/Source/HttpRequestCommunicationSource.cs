using System.Runtime.CompilerServices;
using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Communication.Source
{
  public class HttpRequestCommunicationSource(HttpContext context) : ICommunicationSource
  {
    private readonly HttpContext context = context;

    public async Task<RequestData> ReadData(CancellationToken cancellationToken = default)
    {
      var user = new HttpUser
      {
        Identifier = context.User?.Identifier() ?? string.Empty,
      };

      return new RequestData
      {
        Metadata = new HttpRequestMetadata(context.Request, user),
        Data = Adapt(context.Request, cancellationToken)
      };
    }

    private static async IAsyncEnumerable<DataChunk> Adapt(HttpRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      while (true)
      {
        // TO DO: Consider using a buffer pool to reduce memory allocations for large requests
        var buffer = new byte[ServerCapabilities.MaxChunkSize];
        var bytesRead = await request.Body.ReadAsync(buffer.AsMemory(0, ServerCapabilities.MaxChunkSize), cancellationToken);

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