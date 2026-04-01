using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Communication.Adapters;
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
    StreamingCommunicationManager streamingCommunicationManager,
    IntraServerCommunicationManager intraServerCommunicationManager,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService) : Controller
  {
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly StreamingCommunicationManager streamingCommunicationManager = streamingCommunicationManager;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;

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

      var result = capabilities.SupportsStreaming ?
        await StreamInvocation(method, onInstanceClientConnection ?? externalClientConnection!, onInstanceClientConnection != null, hubClient, cancellationToken) :
        await DirectInvocation(method, onInstanceClientConnection ?? externalClientConnection!, hubClient, cancellationToken);
      
      await result.ApplyResultAsync(HttpContext);
    }

    private async Task<IControllerDefinedResult> DirectInvocation(string method, ClientConnection webSocketClientConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      if (HttpContext.Request.ContentLength > ServerCapabilities.MaxContentLength)
      {
        return new StatusResult(StatusCodes.Status413PayloadTooLarge);
      }

      var communicationManager = new DirectInvocationCommunicationManager(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, sendMetadata: webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      var response = await communicationManager.InvokeAsync(hubClient, method, cancellationToken);
      return new MiddleManClientDirectInvocationResult(response, cancellationToken);
    }

    private async Task<IControllerDefinedResult> StreamInvocation(string method, ClientConnection webSocketClientConnection, bool isSameServerConnection,
     ISingleClientProxy hubClient, CancellationToken cancellationToken)
    {
      var correlation = Guid.NewGuid();

      var adapter = new HttpRequestAdapter(HttpContext.Request, new HttpUser
      {
        Identifier = User.Identifier(),
      }, webSocketClientConnection.ClientCapabilities.SendHTTPMetadata);

      try
      {
        await intraServerCommunicationManager.RegisterRequestSession(correlation, isSameServerConnection);
        await hubClient.SendAsync(method, correlation, cancellationToken);

        return isSameServerConnection ?
          await SameServerStreamInvocation(correlation, adapter, cancellationToken) :
          await IntraServerStreamInvocation(correlation, adapter, cancellationToken);
      }
      finally
      {
        await intraServerCommunicationManager.ClearRequestSession(correlation, isSameServerConnection);
      }
    }

    private async Task<IControllerDefinedResult> SameServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter, CancellationToken cancellationToken)
    {
      await streamingCommunicationManager.WriteAsync(adapter, correlation);

      return new MiddleManClientStreamingResult(streamingCommunicationManager.ReadAsync(correlation), cancellationToken);
    }

    private async Task<IControllerDefinedResult> IntraServerStreamInvocation(Guid correlation, IDataWriterAdapter adapter, CancellationToken cancellationToken)
    {
      await intraServerCommunicationManager.WriteRequestAsync(adapter, correlation, cancellationToken);

      return new MiddleManClientStreamingResult(intraServerCommunicationManager.ReadResponseAsync(correlation, cancellationToken), cancellationToken);
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
