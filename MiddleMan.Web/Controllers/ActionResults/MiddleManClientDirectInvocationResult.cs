using Microsoft.AspNetCore.Mvc;
using MiddleMan.Web.Communication.ClientContracts;

namespace MiddleMan.Web.Controllers.ActionResults
{
public class MiddleManClientDirectInvocationResult : ActionResult
  {
    private readonly DirectInvocationResponse? response;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public MiddleManClientDirectInvocationResult() { }

    public MiddleManClientDirectInvocationResult(DirectInvocationResponse response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
      response?.Metadata?.Apply(context.HttpContext.Response);

      await context.HttpContext.Response.BodyWriter.WriteAsync(response?.Data ?? [], cancellationToken);
      await context.HttpContext.Response.BodyWriter.CompleteAsync();
    }
  }
}
