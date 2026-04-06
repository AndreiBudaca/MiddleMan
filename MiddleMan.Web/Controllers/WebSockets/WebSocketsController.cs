using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.ClientInvocator;
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
    StreamingCommunicationManager streamingCommunicationManager,
    IntraServerCommunicationManager intraServerCommunicationManager,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    ILogger<WebSocketsController> logger) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly StreamingCommunicationManager streamingCommunicationManager = streamingCommunicationManager;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly ILogger<WebSocketsController> logger = logger;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{webSocketClientName}/{method}/{*rest}")]
    public async Task Send([FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(webSocketClientName) || string.IsNullOrWhiteSpace(method))
      {
        await new StatusResult(StatusCodes.Status400BadRequest).ApplyResultAsync(HttpContext);
        return;
      }

      if (HttpContext.Request.ContentLength == null && HttpContext.Request.Method != HttpMethods.Get)
      {
        await new StatusResult(StatusCodes.Status411LengthRequired).ApplyResultAsync(HttpContext);
        return;
      }

      var onInstanceClientConnection = webSocketClientConnectionsService.GetWebSocketClientConnection(User.Identifier(), webSocketClientName);
      var externalClientConnection = onInstanceClientConnection == null ? await GetExternalClientConnection(webSocketClientName) : null;

      var connectionExists = onInstanceClientConnection != null || externalClientConnection != null;
      var connectionIsValid = !string.IsNullOrWhiteSpace(onInstanceClientConnection?.ConnectionId) || !string.IsNullOrWhiteSpace(externalClientConnection?.ConnectionId);

      if (!connectionExists || !connectionIsValid)
      {
        await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
        return;
      }

      var connectionId = onInstanceClientConnection?.ConnectionId ?? externalClientConnection!.ConnectionId!;
      var hubClient = hubContext.Clients.Client(connectionId);
      if (hubClient == null)
      {
        webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName, connectionId);
        await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
        return;
      }

      var capabilities = onInstanceClientConnection?.ClientCapabilities ?? externalClientConnection?.ClientCapabilities ?? new ClientCapabilities();

      IClientInvoker invoker = capabilities.SupportsStreaming ?
        new StreamInvoker(intraServerCommunicationManager, streamingCommunicationManager, logger) :
        new DirectClientInvoker(logger);

      var result = await invoker.Invoke(HttpContext, method, onInstanceClientConnection ?? externalClientConnection!, onInstanceClientConnection != null, hubClient, cancellationToken);
      await result.ApplyResultAsync(HttpContext);
      await invoker.Cleanup();
    }

    private async Task<ClientConnection?> GetExternalClientConnection(string webSocketClientName)
    {
      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(ServerCapabilities.ClientConnectionTimeoutSeconds));
      try
      {
        return await clientInfoCommunicationManager.QueryClientConnection(User.Identifier(), webSocketClientName, cts.Token);
      }
      catch (OperationCanceledException) when (cts.IsCancellationRequested)
      {
        return null;
      }
      finally
      {
        cts.Dispose();
      }
    }
  }
}
