using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Core;
using MiddleMan.Web.Controllers.Authentication.Model;
using MiddleMan.Web.Infrastructure.Identity;
using MiddleMan.Web.Infrastructure.Tokens;
using MiddleMan.Web.Infrastructure.Tokens.Model;

namespace MiddleMan.Web.Controllers.Authentication
{
  [Route("account")]
  [AllowAnonymous]
  public class AccountController(IConfiguration configuration) : Controller
  {
    private readonly IConfiguration configuration = configuration;

    [HttpGet]
    [Route("login")]
    public IActionResult Login()
    {
      if (User.Identity?.IsAuthenticated ?? false)
        return RedirectToAction("Index", "Home");

      return Challenge();
    }

    [HttpGet]
    [Authorize]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
      await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
      return RedirectToAction("Index", "Home");
    }
  }
}

