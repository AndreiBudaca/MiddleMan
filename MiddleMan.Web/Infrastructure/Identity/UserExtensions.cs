using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace MiddleMan.Web.Infrastructure.Identity
{
  public static class UserExtensions
  {
    public static string Identifier(this ClaimsPrincipal user)
    {
      return user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    public static string Name(this ClaimsPrincipal user)
    {
      return user.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value ?? string.Empty;
    }
  }
}
