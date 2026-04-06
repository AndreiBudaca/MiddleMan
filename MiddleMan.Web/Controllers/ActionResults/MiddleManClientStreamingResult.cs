using MiddleMan.Communication.Adapters;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Controllers.ActionResults
{
  public class MiddleManClientStreamingResult : IControllerDefinedResult
  {
    private readonly IAsyncEnumerable<byte[]>? response = null;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public MiddleManClientStreamingResult() { }

    public MiddleManClientStreamingResult(IAsyncEnumerable<byte[]> response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public MiddleManClientStreamingResult(IDataWriterAdapter responseAdapter, CancellationToken cancellationToken)
    {
      this.response = responseAdapter.Adapt();
      this.cancellationToken = cancellationToken;
    }

    public async Task ApplyResultAsync(HttpContext context)
    {
      if (response == null) return;

      try
      {
        var defaultResponseMetadata = new HttpResponseMetadata()
        {
          Headers = new Dictionary<string, string?>()
          {
            ["Content-Type"] = "application/octet-stream",
          },
        };

        var currentEnumeration = response;

        var metadataLengthBytes = await currentEnumeration.EnumerateUntil(4, 0, cancellationToken);
        currentEnumeration = metadataLengthBytes.Next;

        var metadataLength = BitConverter.ToInt32(metadataLengthBytes.Received, 0);

        // Read and apply metadata
        if (metadataLength > 0)
        {
          var metadataBytes = await currentEnumeration.EnumerateUntil(metadataLength, 0, cancellationToken);
          currentEnumeration = metadataBytes.Next;

          var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes.Received, 0, metadataLength);
          var responseMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpResponseMetadata>(metadataJson) ?? defaultResponseMetadata;

          responseMetadata.Apply(context.Response);
        }
        else
        {
          defaultResponseMetadata.Apply(context.Response);
        }

        // Write the rest of the response body
        await foreach (var chunk in currentEnumeration)
        {
          await context.Response.BodyWriter.WriteAsync(chunk, cancellationToken);
        }
      }
      finally
      {
        await context.Response.BodyWriter.CompleteAsync();
        await context.Response.CompleteAsync();
      }
    }
  }
}
