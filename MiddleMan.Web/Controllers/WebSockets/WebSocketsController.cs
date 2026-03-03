using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientInvocationSession;
using MiddleMan.Service.WebSocketClientInvocationSession.Enums;
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
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    IWebSocketClientInvocationSessionService clientInvocationSessionService) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly StreamingCommunicationManager communicationManager = communicationManager;
    private readonly IWebSocketClientInvocationSessionService clientInvocationSessionService = clientInvocationSessionService;

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

      var webSocketClientConnection = await webSocketClientConnectionsService.GetWebSocketClientConnection(User.Identifier(), webSocketClientName);
      if (webSocketClientConnection == null || string.IsNullOrWhiteSpace(webSocketClientConnection.ConnectionId))
      {
        await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
        return;
      }

      var hubClient = hubContext.Clients.Client(webSocketClientConnection.ConnectionId);
      if (hubClient == null)
      {
        await webSocketClientConnectionsService.DeleteWebSocketClientConnection(User.Identifier(), webSocketClientName, webSocketClientConnection.ConnectionId);
        await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
        return;
      }

      if (webSocketClientConnection.ClientCapabilities.SupportsStreaming)
      {
        await StreamInvocation(method, webSocketClientConnection, hubClient, cancellationToken);
      }
      else
      {
        await DirectInvocation(method, webSocketClientConnection, hubClient, cancellationToken);
      }
    }

    private async Task DirectInvocation(string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (HttpContext.Request.ContentLength > ServerCapabilities.MaxContentLength)
      {
        await new StatusResult(StatusCodes.Status413PayloadTooLarge).ApplyResultAsync(HttpContext);
        return;
      }

      var communicationManager = new DirectInvocationCommunicationManager(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, sendMetadata: webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
      await new MiddleManClientDirectInvocationResult(response, cancellationToken).ApplyResultAsync(HttpContext);
    }

    private async Task StreamInvocation(string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      var correlation = Guid.NewGuid();

      await hubClient.SendAsync(method, correlation, cancellationToken);

      var adapter = new HttpRequestAdapter(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      try
      {
        await clientInvocationSessionService.RegisterSession(correlation);

        if (webSocketClientConnection.IsConnectedToCurrentServer)
        {
          await SameServerStreamInvocation(correlation, adapter, cancellationToken);
        }
        else
        {
          await IntraServerStreamInvocation(correlation, adapter, cancellationToken);
        }
      }
      finally
      {
        await clientInvocationSessionService.ClearSession(correlation);
      }
    }

    private async Task SameServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter,
      CancellationToken cancellationToken)
    {
      await Task.WhenAll(
        communicationManager.WriteAsync(adapter, correlation),
        new MiddleManClientStreamingResult(communicationManager.ReadAsync(correlation), cancellationToken).ApplyResultAsync(HttpContext)
      );
    }

    private async Task IntraServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter,
     CancellationToken cancellationToken)
    {
      var intraServerCommunicationManager = new IntraServerCommunicationManager(clientInvocationSessionService);
      var responseDataSource = new IntraServerSessionDataAdapter(clientInvocationSessionService, correlation, SessionDataTypes.Response).Adapt();

      await Task.WhenAll(
        intraServerCommunicationManager.WriteAsync(adapter, correlation, SessionDataTypes.Request),
        new MiddleManClientStreamingResult(responseDataSource, cancellationToken).ApplyResultAsync(HttpContext)
      );
    }
  }
}
