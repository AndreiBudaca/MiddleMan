using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Controllers.ActionResults
{
  public class MiddleManClientResult : IControllerResult
  {
    private readonly HttpResponseMetadata? responseMetadata = null;
    private readonly IAsyncEnumerable<byte[]>? response = null;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public MiddleManClientResult() { }

    public MiddleManClientResult(HttpResponseMetadata? responseMetadata, IAsyncEnumerable<byte[]>? response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.responseMetadata = responseMetadata;
      this.cancellationToken = cancellationToken;
    }

    public async Task ApplyResultAsync(HttpContext context)
    {
      try
      {
        var defaultResponseMetadata = new HttpResponseMetadata()
        {
          Headers = new Dictionary<string, string?>()
          {
            ["Content-Type"] = "application/octet-stream",
          },
        };

        (responseMetadata ?? defaultResponseMetadata).Apply(context.Response);
        if (response == null) return;

        await foreach (var chunk in response.WithCancellation(cancellationToken))
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
