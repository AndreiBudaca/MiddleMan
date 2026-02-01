using Microsoft.AspNetCore.Mvc;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Controllers.ActionResults
{
  public class MiddleManClientResult : ActionResult
  {
    private readonly IAsyncEnumerable<byte[]>? response = null;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public MiddleManClientResult() { }

    public MiddleManClientResult(IAsyncEnumerable<byte[]> response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
      if (response == null) return;

      var defaultResponseMetadata = new HttpResponseMetadata()
      {
        Headers = new List<HttpHeader>()
        {
          new HttpHeader() { Name = "Content-Type", Value = "application/octet-stream" },
        },
      };

      var metadataLengthBytes = await response.EnumerateUntil(4, 0, cancellationToken);
      var metadataLength = BitConverter.ToInt32(metadataLengthBytes.Received, 0);

      // Read and apply metadata
      if (metadataLength > 0)
      {
        var metadataBytes = await response.PrependItems(cancellationToken, metadataLengthBytes.CurrentEnumerationItem)
          .EnumerateUntil(metadataLength, 0, cancellationToken);
        
        var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes.Received, 0, metadataLength);
        var responseMetadata = System.Text.Json.JsonSerializer.Deserialize<HttpResponseMetadata>(metadataJson) ?? defaultResponseMetadata;

        responseMetadata.Apply(context.HttpContext.Response);
        await context.HttpContext.Response.BodyWriter.WriteAsync(metadataBytes.CurrentEnumerationItem, cancellationToken);
      }
      else
      {
        defaultResponseMetadata.Apply(context.HttpContext.Response);
      }

      // Write the rest of the response body
      await foreach (var chunk in response)
        {
          await context.HttpContext.Response.BodyWriter.WriteAsync(chunk, cancellationToken);
        }

      await context.HttpContext.Response.BodyWriter.CompleteAsync();
    }
  }
}
