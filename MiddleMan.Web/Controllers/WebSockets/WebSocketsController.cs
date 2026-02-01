using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Controllers.ActionResults;
using MiddleMan.Web.Hubs;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("api/websockets")]
  [DisableFormValueModelBinding]
  public class WebSocketsController(
    IHubContext<PlaygroundHub> hubContext,
    IWebSocketClientsService webSocketClientsService,
    CommunicationManager communicationManager
    ) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly CommunicationManager communicationManager = communicationManager;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{webSocketClientName}/{method}/{*rest}")]
    public async Task<IActionResult> Send([FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();

      var webSocketClient = await webSocketClientsService.GetWebSocketClient(User.Identifier(), webSocketClientName);
      if (webSocketClient == null) return NotFound();

      if (string.IsNullOrWhiteSpace(webSocketClient.ConnectionId)) return base.NotFound();

      var hubClient = hubContext.Clients.Client(webSocketClient.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName);
        return NotFound();
      }

      var correlation = Guid.NewGuid();

      await hubClient.SendAsync(method, correlation, cancellationToken);

      await communicationManager.WriteAsync(new HttpRequestAdapterAdapter(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }), correlation);

      return new MiddleManClientResult(communicationManager.ReadAsync(correlation), cancellationToken);
    }
  }
}
