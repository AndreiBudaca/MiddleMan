using Microsoft.IdentityModel.Tokens;
using MiddleMan.Web.Infrastructure.Tokens.Constants;
using MiddleMan.Web.Infrastructure.Tokens.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MiddleMan.Web.Infrastructure.Tokens
{
  public static class TokenManager
  {
    public static string Generate(TokenData data)
    {
      ArgumentNullException.ThrowIfNull(nameof(data));
      ArgumentException.ThrowIfNullOrWhiteSpace(data.Secret, nameof(data.Secret));

      var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(data.Secret));
      var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

      List<Claim> userClaims =
      [
        new Claim(ClaimTypes.NameIdentifier, data.Identifier),
        new Claim(ClaimTypes.Name, data.Name)
      ];

      var tokenOptions = new JwtSecurityToken(
          issuer: TokenConstants.TokenIssuer,
          audience: TokenConstants.TokenIssuer,
          claims: userClaims,
          expires: DateTime.Now.AddMinutes(data.Validity),
          signingCredentials: signinCredentials
      );
      return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }
  }
}
