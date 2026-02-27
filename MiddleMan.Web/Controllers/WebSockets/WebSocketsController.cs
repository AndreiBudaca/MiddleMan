using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
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
    StreamingCommunicationManager communicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly StreamingCommunicationManager communicationManager = communicationManager;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{webSocketClientName}/{method}/{*rest}")]
    public async Task<IActionResult> Send([FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName)) return base.NotFound();
      if (string.IsNullOrWhiteSpace(method)) return NotFound();
      if (HttpContext.Request.ContentLength == null && HttpContext.Request.Method != HttpMethods.Get) return new StatusCodeResult(StatusCodes.Status411LengthRequired);

      var webSocketClientConnection = await webSocketClientConnectionsService.GetWebSocketClientConnection(User.Identifier(), webSocketClientName);
      if (webSocketClientConnection == null || string.IsNullOrWhiteSpace(webSocketClientConnection.ConnectionId)) return NotFound();

      var hubClient = hubContext.Clients.Client(webSocketClientConnection.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName, webSocketClientConnection.ConnectionId);
        return NotFound();
      }

      if (webSocketClientConnection.ClientCapabilities.SupportsStreaming)
      {
        return await StreamInvocation(method, webSocketClientConnection, hubClient, cancellationToken);
      }

      return await DirectInvocation(method, webSocketClientConnection, hubClient, cancellationToken);
    }

    private async Task<IActionResult> DirectInvocation(string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (HttpContext.Request.ContentLength > ServerCapabilities.MaxContentLength)
      {
        return new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
      }

      var communicationManager = new DirectInvocationCommunicationManager(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, sendMetadata: webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
      return new MiddleManClientDirectInvocationResult(response, cancellationToken);
    }

    private async Task<IActionResult> StreamInvocation(string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      var correlation = Guid.NewGuid();

      await hubClient.SendAsync(method, correlation, cancellationToken);

      var adapter = new HttpRequestAdapterAdapter(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      await communicationManager.WriteAsync(adapter, correlation);

      return new MiddleManClientStreamingResult(communicationManager.ReadAsync(correlation), cancellationToken);
    }

    private bool RequiresStreaming => HttpContext.Request.ContentLength > ServerCapabilities.MaxContentLength;
  }
}
