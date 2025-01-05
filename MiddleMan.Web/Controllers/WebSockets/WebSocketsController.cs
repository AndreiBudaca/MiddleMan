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
    public async Task<IActionResult> Send([FromRoute] string websocketClientName, [FromRoute] string method, [FromBody] object?[] args, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(websocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();

      var websocketClient = await webSocketClientsService.GetWebSocketClient(User.Identifier(), websocketClientName);
      if (websocketClient == null) return NotFound();

      var methodInfo = websocketClient.Methods.FirstOrDefault(m => m.Name == method);
      if (methodInfo == null) return NotFound();

      if (string.IsNullOrWhiteSpace(websocketClient.ConnectionId)) return base.NotFound();

      var hubClient = hubContext.Clients.Client(websocketClient.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientsService.DeleteWebSocketClient(User.Identifier(), websocketClientName);
        return NotFound();
      }

      if (methodInfo.Returns != null)
      {
        var result = await hubClient.InvokeCoreAsync<object>(method, args, cancellationToken);
        return Ok(result);
      }
      else
      {
        await hubClient.SendCoreAsync(method, args, cancellationToken);
        return NoContent();
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
      var clients = await webSocketClientsService.GetWebSocketClients(User.Identifier());

      var model = clients.Select(c => new WebSocketClientModel
      {
        Name = c.Name,
        Methods = c.Methods.Select(m => new WebSocketClientMethodModel
        {
          Name = m.Name,
          Arguments = m.Arguments.Select(x => new WebSocketClientMethodArgumentModel(x)).ToList(),
          Returns = m.Returns != null ? new WebSocketClientMethodArgumentModel(m.Returns) : null
        }).ToList()
      }).ToList();

      return PartialView(model);
    }
  }
}
