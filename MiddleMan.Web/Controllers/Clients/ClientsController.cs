using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Service.WebSocketClients.Dto;
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
      var model = clients.Select(BuildModel);

      return Ok(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] NewClientModel model)
    {
      if (await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), model.Name))
      {
        return Conflict("A client with the same name already exists");
      }

      var newClient = await webSocketClientsService.AddWebSocketClient(new NewWebSockerClientDto
      {
        Name = model.Name,
        Identifier = User.Identifier(),
      });

      return Created((string?)null, BuildModel(newClient));
    }

    [HttpDelete("{clientName}")]
    public async Task<IActionResult> DeleteClient([FromRoute] string clientName)
    {
      if (!await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), clientName))
      {
        return NotFound();
      }

      await webSocketClientsService.DeleteWebSocketClient(User.Identifier(), clientName);

      return NoContent();
    }

    [HttpPost("{clientName}/token")]
    public async Task<IActionResult> GenerateNewToken([FromRoute] string clientName)
    {
      if (!await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), clientName))
      {
        return NotFound();
      }

      await webSocketClientsService.DeleteWebSocketClientConnection(User.Identifier(), clientName);

      var token = TokenManager.Generate(new TokenData
      {
        Identifier = User.Identifier(),
        Name = clientName,
        Secret = configuration.GetValue<string>(ConfigurationConstants.Authentication.ClientToken.Secret),
      });

      var hash = await webSocketClientsService.UpdateWebSocketClientToken(User.Identifier(), clientName, token);

      return Ok(new WebSocketTokenDataModel
      {
        Token = token,
        TokenHash = hash != null ? Convert.ToBase64String(hash) : null,
      });
    }

    [HttpDelete("{clientName}/token")]
    public async Task<IActionResult> DeleteClientToken([FromRoute] string clientName)
    {
      if (!await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), clientName))
      {
        return NotFound();
      }

      await webSocketClientsService.UpdateWebSocketClientToken(User.Identifier(), clientName, null);
      await webSocketClientsService.DeleteWebSocketClientConnection(User.Identifier(), clientName);

      return Ok(new WebSocketTokenDataModel());
    }

    private static WebSocketClientModel BuildModel(WebSocketClientDto client)
    {
      return new WebSocketClientModel
      {
        Name = client.Name,
        MethodsUrl = client.MethodsUrl,
        IsConnected = !string.IsNullOrEmpty(client.ConnectionId),
        LastConnectedAt = client.LastConnectedAt,
        Signature = client.Signature != null ? Convert.ToBase64String(client.Signature) : null,
        TokenHash = client.TokenHash != null ? Convert.ToBase64String(client.TokenHash) : null,
      };
    }
  }
}
