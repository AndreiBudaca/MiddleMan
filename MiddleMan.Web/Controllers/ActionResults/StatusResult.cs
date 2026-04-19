namespace MiddleMan.Web.Controllers.ActionResults
{
  public class StatusResult(int statusCode) : IControllerResult
  {
    private readonly int statusCode = statusCode;

    public Task ApplyResultAsync(HttpContext context)
    {
      context.Response.StatusCode = statusCode;
      return Task.CompletedTask;
    }
  }
}