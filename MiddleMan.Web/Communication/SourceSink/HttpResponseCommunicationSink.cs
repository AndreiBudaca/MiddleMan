using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.SourceSink
{
  public class HttpResponseCommunicationSink(HttpResponse response) : ICommunicationSourceSink
  {
    private readonly HttpResponse response = response;

    public async Task WriteData(ResponseData? data, CancellationToken cancellationToken = default)
    {
      try
      {
        (data?.Metadata ?? DefaultResponseMetadata).Apply(response);
        if (response == null || data?.Data == null) return;

        await foreach (var chunk in data.Data.WithCancellation(cancellationToken))
        {
          await response.BodyWriter.WriteAsync(chunk.Data.AsMemory(0, chunk.Length), cancellationToken);
        }
      }
      finally
      {
        await response.BodyWriter.CompleteAsync();
        await response.CompleteAsync();
      }
    }

    private static HttpResponseMetadata DefaultResponseMetadata => new()
    {
      Headers = new Dictionary<string, string?>()
      {
        ["Content-Type"] = "application/octet-stream",
      },
    };
  }
}
