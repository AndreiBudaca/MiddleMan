using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MiddleMan.Web.Infrastructure.Attributes
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
  {
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
      var factories = context.ValueProviderFactories;

      // Remove the factories that read form data
      factories.RemoveType<FormValueProviderFactory>();
      factories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
      // No action needed after execution
    }
  }
}
