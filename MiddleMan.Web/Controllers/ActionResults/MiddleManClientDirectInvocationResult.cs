using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Controllers.ActionResults
{
public class MiddleManClientDirectInvocationResult : IControllerResult
  {
    private readonly DirectInvocationResponse? response;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public MiddleManClientDirectInvocationResult() { }

    public MiddleManClientDirectInvocationResult(DirectInvocationResponse response, CancellationToken cancellationToken)
    {
      this.response = response;
      this.cancellationToken = cancellationToken;
    }

    public async Task ApplyResultAsync(HttpContext context)
    {
      var metadata = response?.Metadata ?? new HttpResponseMetadata
      {
        Headers = new Dictionary<string, string?>()
        {
          ["Content-Type"] = "application/octet-stream",
        },
      };

      metadata.Apply(context.Response);

      await context.Response.BodyWriter.WriteAsync(response?.Data ?? [], cancellationToken);
      await context.Response.BodyWriter.CompleteAsync();
    }
  }
}
