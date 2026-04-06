namespace MiddleMan.Web.Controllers.ActionResults
{
  public class StatusResult(int statusCode) : IControllerDefinedResult
  {
    private readonly int statusCode = statusCode;

    public Task ApplyResultAsync(HttpContext context)
    {
      context.Response.StatusCode = statusCode;
      return Task.CompletedTask;
    }
  }
}