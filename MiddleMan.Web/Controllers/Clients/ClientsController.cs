using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
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
    IConfiguration configuration,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    ClientInfoCommunicationManager clientInfoCommunicationManager) : Controller
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IConfiguration configuration = configuration;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyOwned = false)
    {
      var clients = await webSocketClientsService.GetWebSocketClients(User.Identifier(), User.Email(), onlyOwned);
      var model = clients.Select(BuildModel);

      return Ok(model);
    }

    [HttpGet("connection-status")]
    public async Task<IActionResult> GetConnectionStatus()
    {
      var clients = await webSocketClientsService.GetWebSocketClients(User.Identifier(), User.Email());

      var clientConnectionStatuses = await Task.WhenAll(clients
        .Where(c => c != null && c.Name != null && c.UserId != null)
        .Select(async client =>
        {
          var clientConnection = webSocketClientConnectionsService.GetWebSocketClientConnection(client.UserId!, client.Name!);

          if (clientConnection == null && ServerCapabilities.ClusterMode)
          {
            clientConnection = await clientInfoCommunicationManager.QueryClientConnection(client.UserId!, client.Name!);
          }

          return new WebSocketClientConnectionStatusModel
          {
            UserId = client.UserId,
            Name = client.Name,
            IsConnected = clientConnection != null,
          };
        }
      ));

      return Ok(clientConnectionStatuses);
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

    [HttpPost("{clientName}/share")]
    public async Task<IActionResult> AddClientShare([FromRoute] string clientName, [FromBody] WebSocketSharedWithModel model)
    {
      if (!await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), clientName))
      {
        return NotFound();
      }

      if (await webSocketClientsService.ExistsWebSocketClientShares(User.Identifier(), clientName, model.SharedWithUserEmail))
      {
        return Conflict("A share with the same email already exists");
      }

      await webSocketClientsService.AddWebSocketClientShare(User.Identifier(), clientName, model.SharedWithUserEmail);

      return Ok();
    }

    [HttpDelete("{clientName}/share/{sharedWithUserEmail}")]
    public async Task<IActionResult> DeleteClientShare([FromRoute] string clientName, [FromRoute] string sharedWithUserEmail)
    {
      if (!await webSocketClientsService.ExistsWebSocketClients(User.Identifier(), clientName))
      {
        return NotFound();
      }

      if (!await webSocketClientsService.ExistsWebSocketClientShares(User.Identifier(), clientName, sharedWithUserEmail))
      {
        return NotFound();
      }

      await webSocketClientsService.DeleteWebSocketClientShare(User.Identifier(), clientName, sharedWithUserEmail);

      return NoContent();
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

      // TO DO
      // await webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), clientName);

      var token = TokenManager.Generate(new TokenData
      {
        Identifier = User.Identifier(),
        Name = clientName,
        Email = User.Email(),
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
      // TO DO
      // await webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), clientName);

      return Ok(new WebSocketTokenDataModel());
    }

    private static WebSocketClientModel BuildModel(WebSocketClientDto client)
    {
      return new WebSocketClientModel
      {
        UserId = client.UserId,
        Name = client.Name,
        MethodsUrl = client.MethodsUrl,
        Signature = client.Signature != null ? Convert.ToBase64String(client.Signature) : null,
        TokenHash = client.TokenHash != null ? Convert.ToBase64String(client.TokenHash) : null,
        SharedWithUserEmails = client.SharedWithUserEmails,
      };
    }
  }
}
