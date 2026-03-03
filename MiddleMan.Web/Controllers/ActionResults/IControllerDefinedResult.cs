namespace MiddleMan.Web.Controllers.ActionResults
{
  public interface IControllerDefinedResult
  {
    Task ApplyResultAsync(HttpContext context);
  }
}