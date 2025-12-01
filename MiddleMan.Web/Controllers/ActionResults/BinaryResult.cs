using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Web.Controllers.ActionResults
{
  public class BinaryResult : ActionResult
  {
    private readonly IAsyncEnumerable<byte[]>? response = null;
    private readonly CancellationToken? cancellationToken = null;

    public BinaryResult() { }

    public BinaryResult(IAsyncEnumerable<byte[]> response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
      context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
      context.HttpContext.Response.Headers.ContentType = "application/octet-stream";
      if (response == null) return;

      await foreach (var chunk in response)
      {
        await context.HttpContext.Response.BodyWriter.WriteAsync(chunk, cancellationToken ?? CancellationToken.None);
      }

      await context.HttpContext.Response.BodyWriter.CompleteAsync();
    }
  }
}
