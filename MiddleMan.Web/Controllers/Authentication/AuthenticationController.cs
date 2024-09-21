using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Core;
using MiddleMan.Core.Tokens;
using MiddleMan.Core.Tokens.Model;
using MiddleMan.Web.Controllers.Authentication.Model;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Controllers.Authentication
{
    [Route("[controller]")]
  public class AuthenticationController : Controller
  {
    private readonly IConfiguration configuration;

    public AuthenticationController(IConfiguration configuration)
    {
      this.configuration = configuration;
    }

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
        Validity = TokenManager.DefaultValidity,
        Secret = configuration.GetValue<string>(ConfigurationConstants.Authentication.ClientToken.Secret) ?? string.Empty,
      });

      return Ok(token);
    }

    [HttpGet]
    [ClientToken]
    [Route("Test")]
    public IActionResult TestClientLogin()
    {
      return Ok();
    }
  }
}
