using MiddleMan.Core;
using MiddleMan.Core.Extensions;

namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpResponseMetadata
  {
    public int ResponseCode { get; set; } = 200;

    public Dictionary<string, string?> Headers { get; set; } = [];

    public HttpResponseMetadata() { }

    public HttpResponseMetadata(int responseCode)
    {
      ResponseCode = responseCode;
    }

    public void Apply(HttpResponse response)
    {
      response.StatusCode = ResponseCode;
      foreach (var header in Headers)
      {
        response.Headers[header.Key] = header.Value;
      }
    }

    public async static Task<(HttpResponseMetadata?, IAsyncEnumerable<DataChunk>)> ReadFromChunkedData(IAsyncEnumerable<DataChunk> dataStream, CancellationToken cancellationToken = default)
    {
      var lengthResult = await dataStream.EnumerateUntil(4, 0, cancellationToken);
      dataStream = lengthResult.Next;

      var length = BitConverter.ToInt32(lengthResult.Received.Data);

      if (length == 0) return (null, dataStream);

      var metadataResult = await dataStream.EnumerateUntil(length, 0, cancellationToken);
      dataStream = metadataResult.Next;

      var metadata = System.Text.Json.JsonSerializer.Deserialize<HttpResponseMetadata>(metadataResult.Received.Data);
      return (metadata, dataStream);
    }
  }
}
