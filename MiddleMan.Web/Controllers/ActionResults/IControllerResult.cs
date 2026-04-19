namespace MiddleMan.Web.Controllers.ActionResults
{
  public interface IControllerResult
  {
    Task ApplyResultAsync(HttpContext context);
  }
}