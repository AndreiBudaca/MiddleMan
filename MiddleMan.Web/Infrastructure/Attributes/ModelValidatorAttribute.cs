using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiddleMan.Web.Infrastructure.Attributes
{
  public class ModelValidatorAttribute : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
    {
      if (!actionExecutingContext.ModelState.IsValid)
      {
        actionExecutingContext.Result = new BadRequestObjectResult(actionExecutingContext.ModelState);
      }
    }
  }
}
