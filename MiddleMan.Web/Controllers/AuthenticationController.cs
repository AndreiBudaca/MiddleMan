using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Web.Controllers
{
  [Route("[controller]")]
  public class AuthenticationController : Controller
  {
    [HttpGet]
    [AllowAnonymous]
    [Route("Login")]
    public IActionResult Login()
    {
      if (User.Identity?.IsAuthenticated ?? false)
        return RedirectToAction("Index", "Home"); 

      return Challenge();
    }

    [HttpGet]
    [Route("Logout")]
    public async Task<IActionResult> Logout()
    {
      await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
      return RedirectToAction("Index", "Home");
    }
  }
}
