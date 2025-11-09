using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Controllers.Authentication.Model;
using MiddleMan.Web.Controllers.Clients.Model;
using MiddleMan.Web.Infrastructure.Identity;
using MiddleMan.Web.Infrastructure.Tokens;
using MiddleMan.Web.Infrastructure.Tokens.Model;

namespace MiddleMan.Web.Controllers.Clients
{
  [Authorize]
  [Route("api/clients")]
  public class ClientsController(
    IWebSocketClientsService webSocketClientsService,
    IConfiguration configuration
    ) : Controller
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IConfiguration configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
      var clients = await webSocketClientsService.GetWebSocketClients(User.Identifier());

      var model = clients.Select(client => new WebSocketClientDetailsModel
      {
        Name = client.Name,
        MethodsUrl = client.MethodsUrl,
        IsConnected = client.IsConnected,
      });

      return Ok(model);
    }

    [HttpGet]
    [Route("{websocketClientName}")]
    public async Task<IActionResult> GetClientDetails(string websocketClientName)
    {
      var client = await webSocketClientsService.GetWebSocketClient(User.Identifier(), websocketClientName);

      if (client == null) return NotFound();

      var model = new WebSocketClientDetailsModel
      {
        Name = client.Name,
        MethodsUrl = client.MethodsUrl,
        IsConnected = client.IsConnected,
      };

      return Ok(model);
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
