using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MiddleMan.Core;
using MiddleMan.Core.Tokens;

namespace MiddleMan.Web.Infrastructure.Attributes
{
  public class ClientTokenAttribute : TypeFilterAttribute
  {
    public ClientTokenAttribute() : base(typeof(ClientTokenFilter)) { }
  }

  public class ClientTokenFilter : IAuthorizationFilter
  {
    private readonly IConfiguration configuration;

    public ClientTokenFilter(IConfiguration configuration)
    {
      this.configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
      var auth = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
      if (auth == null)
      {
        context.Result = new UnauthorizedResult();
        return;
      }

      var authParts = auth.Split(" ", StringSplitOptions.RemoveEmptyEntries);
      if (authParts.Length != 2)
      {
        context.Result = new UnauthorizedResult();
        return;
      }

      var authType = authParts[0];
      var authToken = authParts[1];

      if (authType != "Bearer")
      {
        context.Result = new UnauthorizedResult();
        return;
      }

      var secret = configuration.GetValue<string>(ConfigurationConstants.Authentication.ClientToken.Secret) ?? string.Empty;
      var token = TokenManager.Parse(authToken, secret);
      if (token == null)
      {
        context.Result = new ForbidResult();
      }
    }
  }
}
