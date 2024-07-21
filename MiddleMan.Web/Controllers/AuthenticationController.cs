using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Web.Controllers
{
  [Route("[controller]")]
  public class AuthenticationController : Controller
  {
    [AllowAnonymous]
    [Route("Login")]
    public IActionResult Login()
    {
      if (User.Identity?.IsAuthenticated ?? false)
        return RedirectToAction("Index", "Home"); 

      return Challenge();
    }
  }
}
