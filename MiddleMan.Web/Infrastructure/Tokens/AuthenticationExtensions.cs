using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using MiddleMan.Core;
using MiddleMan.Web.Infrastructure.Tokens.Constants;
using System.Text;

namespace MiddleMan.Web.Infrastructure.Tokens
{
  public static class AuthenticationExtensions
  {
    public static AuthenticationBuilder AddJWTAuthentication(this AuthenticationBuilder builder, ConfigurationManager configuration)
    {
      return builder.AddJwtBearer(options =>
      {
        var signingKey = configuration.GetValue<string>(ConfigurationConstants.Authentication.ClientToken.Secret) ?? string.Empty;

        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = TokenConstants.TokenIssuer,
          ValidAudience = TokenConstants.TokenIssuer,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
      });
    }
  }
}
