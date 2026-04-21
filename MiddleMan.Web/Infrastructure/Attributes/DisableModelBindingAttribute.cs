using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MiddleMan.Web.Infrastructure.Attributes
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  public class DisableModelBindingAttribute : Attribute, IResourceFilter
  {
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
      var factories = context.ValueProviderFactories;

      // Keep only non-body value providers so request stream remains untouched.
      factories.Clear();
      factories.Add(new RouteValueProviderFactory());
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
      // No action needed after execution
    }
  }
}
