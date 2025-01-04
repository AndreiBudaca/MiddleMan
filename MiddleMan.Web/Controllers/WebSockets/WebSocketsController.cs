using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Hubs;
using MiddleMan.Web.Infrastructure.Identity;
using System.Text;

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

      object result;
      var reqBody = HttpContext.Request.Body;
      if (reqBody == null)
      {
        result = await hubClient.InvokeAsync<object>(method, cancellationToken);
        return Ok(result);
      }

      var buff = new byte[(int)(HttpContext.Request.ContentLength ?? 0)];
      await reqBody.ReadAsync(buff, cancellationToken);
      var data = Encoding.UTF8.GetString(buff);

      result = await hubClient.InvokeAsync<object>(method, data, cancellationToken);
      return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
      return Ok(await webSocketClientsService.GetWebSocketClients(User.Identifier()));
    }
  }
}
