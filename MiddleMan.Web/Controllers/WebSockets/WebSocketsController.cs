using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Communication.ClientInvocator;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Controllers.ActionResults;
using MiddleMan.Web.Hubs;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Identity;
using MiddleMan.Web.Resiliency;

namespace MiddleMan.Web.Controllers.WebSockets
{
  [Authorize]
  [Route("client-portal")]
  [DisableModelBinding]
  public class WebSocketsController(
    IWebSocketClientsService webSocketClientsService,
    IHubContext<PlaygroundHub> hubContext,
    StreamingCommunicationManager streamingCommunicationManager,
    IntraServerCommunicationManager intraServerCommunicationManager,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    ILogger<WebSocketsController> logger) : Controller
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IHubContext<PlaygroundHub> hubContext = hubContext;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly StreamingCommunicationManager streamingCommunicationManager = streamingCommunicationManager;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly ILogger<WebSocketsController> logger = logger;

    [RequestSizeLimit(1_000_000_000)]
    [Route("{userId}/{webSocketClientName}/{method}/{*rest}")]
    public async Task Send([FromRoute] string userId, [FromRoute] string webSocketClientName, [FromRoute] string method, CancellationToken cancellationToken)
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

      if (userId != null && userId != User.Identifier())
      {
        var isAllowed = await webSocketClientsService.ExistsWebSocketClientShares(userId, webSocketClientName, User.Email());

        if (!isAllowed)
        {
          await new StatusResult(StatusCodes.Status403Forbidden).ApplyResultAsync(HttpContext);
          return;
        }
      }

      var (bufferedRequest, metadata) = ProcessRequest(HttpContext.Request);
      IContentBuffer? bufferedResponse = null;

      var retryCount = 1;
      var wasSuccessfulInvocation = false;

      using var communicationFailedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      do
      {
        if (retryCount > 1)
        {
          logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}", webSocketClientName, method, retryCount, ServerCapabilities.MaxRetryAttempts);
        }

        communicationFailedCts.TryReset();
        var clientConnection = await GetClientConnection(userId ?? User.Identifier(), webSocketClientName, communicationFailedCts.Token);

        if (string.IsNullOrWhiteSpace(clientConnection?.ConnectionId))
        {
          await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
          return;
        }

        var hubClient = hubContext.Clients.Client(clientConnection.ConnectionId);
        if (hubClient == null)
        {
          webSocketClientConnectionsService.DeleteWebSocketClientConnection(userId ?? User.Identifier(), webSocketClientName, clientConnection.ConnectionId);
          // TO DO: also delete from other servers in cluster
          await new StatusResult(StatusCodes.Status404NotFound).ApplyResultAsync(HttpContext);
          return;
        }

        logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", webSocketClientName, method, clientConnection.ConnectionId);

        IClientInvoker invoker = clientConnection.ClientCapabilities.SupportsStreaming ?
          new StreamInvoker(intraServerCommunicationManager, streamingCommunicationManager, logger) :
          new DirectClientInvoker(logger);

        try
        {
          var (responseMetadata, responseData) = await invoker.Invoke(bufferedRequest.Read(communicationFailedCts.Token), metadata, method, clientConnection, hubClient, communicationFailedCts.Token);
          bufferedResponse = await BufferResponse(responseData, communicationFailedCts.Token);

          var result = new MiddleManClientResult(responseMetadata, bufferedResponse?.Read(communicationFailedCts.Token), communicationFailedCts.Token);
          await result.ApplyResultAsync(HttpContext);

          await invoker.Cleanup();
          await bufferedRequest.DisposeAsync();
          if (bufferedResponse != null) await bufferedResponse.DisposeAsync();

          logger.LogInformation("Completed invocation for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", webSocketClientName, method, clientConnection.ConnectionId);
          return;
        }
        catch (Exception ex)
        {
          logger.LogError("Invocation error for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}. Error: {ErrorMessage}", webSocketClientName, method, clientConnection.ConnectionId, ex.Message);

          await communicationFailedCts.CancelAsync();
          if (bufferedResponse != null)
          {
            await bufferedResponse.DisposeAsync();
            bufferedResponse = null;
          }

          await invoker.Cleanup();
        }
      } while (!wasSuccessfulInvocation && ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

      await bufferedRequest.DisposeAsync();
      if (bufferedResponse != null) await bufferedResponse.DisposeAsync();

      await new StatusResult(StatusCodes.Status500InternalServerError).ApplyResultAsync(HttpContext);
    }

    private static (IContentBuffer Buffer, HttpRequestMetadata Metadata) ProcessRequest(HttpRequest request)
    {
      var requestAdaptor = new HttpRequestAdaptor(request);
      var metadata = new HttpRequestMetadata(request, new HttpUser
      {
        Identifier = request.HttpContext.User.Identifier(),
      });

      IContentBuffer buffer = ServerCapabilities.FaultToleranceEnabled ?
       new HybridBuffer(requestAdaptor.Adapt(), ServerCapabilities.MaxMemoryBufferSize) :
       new NoBuffer(requestAdaptor.Adapt());

      return (buffer, metadata);
    }

    private async static Task<IContentBuffer?> BufferResponse(IAsyncEnumerable<byte[]>? responseData, CancellationToken cancellationToken)
    {
      if (responseData == null) return null;

      if (ServerCapabilities.FaultToleranceEnabled)
      {
        var buffer = new HybridBuffer(responseData, ServerCapabilities.MaxMemoryBufferSize);

        // Ensure the response is buffered before returning
        await foreach (var _ in buffer.Read(cancellationToken)) { }
        return buffer;
      }

      return new NoBuffer(responseData);
    }

    private async Task<ClientConnection?> GetClientConnection(string userId, string webSocketClientName, CancellationToken cancellationToken = default)
    {
      var clientConnection = webSocketClientConnectionsService.GetWebSocketClientConnection(userId, webSocketClientName);

      if (clientConnection == null && ServerCapabilities.ClusterMode)
      {
        clientConnection = await clientInfoCommunicationManager.QueryClientConnection(userId, webSocketClientName, cancellationToken);

        // double check after querying other servers in cluster (in case the client connection was established while we were querying other servers)
        clientConnection ??= webSocketClientConnectionsService.GetWebSocketClientConnection(userId, webSocketClientName);
      }

      return clientConnection;
    }
  }
}
