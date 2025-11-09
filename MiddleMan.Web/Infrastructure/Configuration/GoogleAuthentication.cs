using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MiddleMan.Core;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class GoogleAuthentication
  {
    public static AuthenticationBuilder AddGoogleAuthentication(this AuthenticationBuilder builder, ConfigurationManager configuration)
    {
      return builder
        .AddCookie(o =>
        {
          o.LoginPath = "/api/account/login";
          o.LogoutPath = "/api/account/logout";
          o.Events.OnRedirectToLogin = (context) =>
          {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
          };
        })
        .AddGoogle(googleOptions =>
        {
          googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          googleOptions.ClientId = configuration[ConfigurationConstants.Authentication.GoogleAuthentication.ClientId] ?? string.Empty;
          googleOptions.ClientSecret = configuration[ConfigurationConstants.Authentication.GoogleAuthentication.ClientSecret] ?? string.Empty;
        });
    }
  }
}
