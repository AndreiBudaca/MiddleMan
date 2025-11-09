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
  [Route("[controller]")]
  [Authorize]
  public class AccountController : Controller
  {
    private readonly IConfiguration configuration;

    public AccountController(IConfiguration configuration) => this.configuration = configuration;

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

    [HttpGet]
    [Route("ClientLogin")]
    public IActionResult GetClientLogin()
    {
      return View();
    }

    [HttpPost]
    [Route("ClientLogin")]
    public IActionResult PostClientLogin([FromBody] ClientLoginModel model)
    {
      var token = TokenManager.Generate(new TokenData
      {
        Identifier = User.Identifier(),
        Name = model.ClientName,
        Secret = configuration.GetValue<string>(ConfigurationConstants.Authentication.ClientToken.Secret),
      });

      return Ok(token);
    }
  }
}

