using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClientConnections;
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
    CommunicationManager communicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly CommunicationManager communicationManager = communicationManager;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{webSocketClientName}/{method}/{*rest}")]
    public async Task<IActionResult> Send([FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();

      var webSocketClientConnection = await webSocketClientConnectionsService.GetWebSocketClientConnection(User.Identifier(), webSocketClientName);
      if (webSocketClientConnection == null) return NotFound();

      var hubClient = hubContext.Clients.Client(webSocketClientConnection);
      if (hubClient == null)
      {
        await webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName, webSocketClientConnection);
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
