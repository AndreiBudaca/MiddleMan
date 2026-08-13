using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Web.Controllers.Authentication
{
  [Route("api/account")]
  public class AccountController : Controller
  {
    [HttpGet]
    [AllowAnonymous]
    [Route("login")]  
    public IActionResult Login()
    {
      if (User.Identity?.IsAuthenticated ?? false) return Redirect("/");

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

