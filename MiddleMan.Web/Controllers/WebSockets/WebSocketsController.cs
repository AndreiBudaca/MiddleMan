using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Controllers.WebSockets.Model;
using MiddleMan.Web.Hubs;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("[controller]")]
  public class WebSocketsController(
    IHubContext<PlaygroundHub> hubContext,
    IWebSocketClientsService webSocketClientsService
    ) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;

    [HttpPost]
    [Route("{websocketClientName}/{method}")]
    public async Task<IActionResult> Send([FromRoute] string websocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(websocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();

      var websocketClient = await webSocketClientsService.GetWebSocketClient(User.Identifier(), websocketClientName);
      if (websocketClient == null) return NotFound();

      if (string.IsNullOrWhiteSpace(websocketClient.ConnectionId)) return base.NotFound();

      var hubClient = hubContext.Clients.Client(websocketClient.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientsService.DeleteWebSocketClient(User.Identifier(), websocketClientName);
        return NotFound();
      }

      using var reader = new StreamReader(Request.Body);
      var bytes = new byte[Request.ContentLength ?? 0];
      await Request.Body.ReadAsync(bytes, cancellationToken);

      var result = await hubClient.InvokeCoreAsync<byte[]>(method, [bytes], cancellationToken);
     
      return Ok(Convert.ToBase64String(result));
    }

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
  }
}
