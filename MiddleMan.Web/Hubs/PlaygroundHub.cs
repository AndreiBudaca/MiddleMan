using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Protocols;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication.ClientContracts;
using MiddleMan.Web.Communication.ClientInvocator;
using MiddleMan.Web.Communication.Metadata;
using MiddleMan.Web.Communication.Metadata.Constants;
using MiddleMan.Web.Hubs.Models;
using MiddleMan.Web.Infrastructure.Identity;
using MiddleMan.Web.Resiliency;
using Polly;
using System.Threading.Channels;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService,
    IWebSocketClientMethodService webSocketClientMethodService,
    StreamingCommunicationManager communicationManager,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IntraServerCommunicationManager intraServerCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    ILogger<PlaygroundHub> logger) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly StreamingCommunicationManager communicationManager = communicationManager;
    private readonly ILogger<PlaygroundHub> logger = logger;

    #region [Clinet connection hooks]
    public override async Task OnConnectedAsync()
    {
      var (id, name, _, clientToken, clientId) = GetClientInfoFromContext();

      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client connection attempt without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        Context.Abort();
        return;
      }

      var isAuthorized = await AuthorizeClient(id, name, clientToken);
      if (!isAuthorized)
      {
        Context.Abort();
        return;
      }

      var onServerConnectionCount = webSocketClientConnectionsService.AddWebSocketClientConnection(id, name, clientId, Context.ConnectionId);
      if (onServerConnectionCount == 1 && ServerCapabilities.ClusterMode)
      {
        await clientInfoCommunicationManager.StartListening(id, name);
      }

      logger.LogInformation("Client connected: {Id} - {Name}. Assigning connection ID: {ConnectionId}. Total connections: {TotalConnections}", id, name, Context.ConnectionId, onServerConnectionCount);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client disconnection without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        return;
      }

      var onServerConnectionCount = webSocketClientConnectionsService.DeleteWebSocketClientConnection(id, name, clientId);
      if (onServerConnectionCount == 0 && ServerCapabilities.ClusterMode)
      {
        await clientInfoCommunicationManager.StopListening(id, name);
      }

      logger.LogInformation("Client disconnected: {Id} - {Name}. Connection ID: {ConnectionId}. Total connections: {TotalConnections}", id, name, Context.ConnectionId, onServerConnectionCount);
    }

    private async Task<bool> AuthorizeClient(string id, string name, string? clientToken)
    {
      if (clientToken == null || !clientToken.StartsWith("Bearer ")) return false;
      return await webSocketClientsService.IsValidWebSocketClientToken(id, name, clientToken[7..]);
    }
    #endregion

    #region [Server info methods]
    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task MethodsRaw(byte[] methodsBytes)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, methodsBytes, CancellationToken.None);
    }

    public async Task<ServerInfoModel> Negociate(ClientInfoModel clientInfo)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      if (clientInfo == null) return new ServerInfoModel { IsAccepted = false };
      if (!ServerCapabilities.AllowedVersions.Contains(clientInfo.Version)) return new ServerInfoModel { IsAccepted = false };

      webSocketClientConnectionsService.AddWebSockerClientConnectionCapabilities(
        id,
        name,
        clientId!,
        new ClientCapabilities
        {
          Version = clientInfo.Version,
          SupportsStreaming = clientInfo.SupportsStreaming,
          SendHTTPMetadata = clientInfo.SendHTTPMetadata,
        }
      );

      var client = await webSocketClientsService.GetWebSocketClient(id, name);
      return new ServerInfoModel
      {
        IsAccepted = true,
        MaxMessageLength = ServerCapabilities.MaxChunkSize,
        MethodSignature = client?.Signature,
      };
    }
    #endregion

    #region [Communication methods]
    public async Task AddReadChannel(Guid correlation, ChannelReader<byte[]> channelReader)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      var sameServerInvocation = intraServerCommunicationManager.ExistsRequestSession(correlation);
      await communicationManager.RegisterSessionReaderChannelAsync(channelReader, correlation, sameServerInvocation);

      if (!sameServerInvocation)
      {
        try
        {
          await intraServerCommunicationManager.RegisterResponseSession(correlation);

          await communicationManager.WriteAsync(intraServerCommunicationManager.ReadRequestAsync(correlation), correlation);
          await intraServerCommunicationManager.WriteResponseAsync(communicationManager.ReadAsync(correlation), correlation);
        }
        finally
        {
          await intraServerCommunicationManager.ClearResponseSession(correlation);
        }
      }
    }

    public async Task<ChannelReader<byte[]>> SubscribeToServer(Guid correlation)
    {
      var (id, name, _, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1));
      await communicationManager.RegisterSessionWriterChannelAsync(channel.Writer, correlation);

      return channel.Reader;
    }

    public async Task<DirectInvocationResponse> Invoke(string? userId, string clientName, string method, DirectInvocationData clientData)
    {
      var (id, _, email, _, _) = GetClientInfoFromContext();

      if (userId != null && userId != id)
      {
        var isAllowed = await webSocketClientsService.ExistsWebSocketClientShares(userId, clientName, email);
        if (!isAllowed) throw new HubException("Not allowed to invoke this method");
      }

      var retryCount = 1;
      var wasSuccessfulInvocation = false;
      using var communicationFailedCts = new CancellationTokenSource();

      do
      {
        if (retryCount > 1)
        {
          logger.LogWarning("Retrying invocation for {WebSocketClientName}, method: {Method}. Attempt {RetryCount} of {MaxRetryAttempts}", clientName, method, retryCount, ServerCapabilities.MaxRetryAttempts);
        }

        communicationFailedCts.TryReset();
        var clientConnection = await GetClientConnection(userId ?? id, clientName, communicationFailedCts.Token);

        if (string.IsNullOrWhiteSpace(clientConnection?.ConnectionId)) throw new HubException("Client connection not found");

        var hubClient = Clients.Client(clientConnection.ConnectionId);
        if (hubClient == null)
        {
          webSocketClientConnectionsService.DeleteWebSocketClientConnection(userId ?? id, clientName, clientConnection.ConnectionId);
          // TO DO: also delete from other servers in cluster
          throw new HubException("Client connection not found");
        }

        if (ServerCapabilities.VerboseLogging)
        {
          logger.LogInformation("Invoking {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
        }

        var invoker = new DirectClientInvoker(logger);

        try
        {
          var (responseMetadata, responseData) = await invoker.Invoke(clientData.Data.AsAsyncEnumerable(), ProcessMetadata(clientData, Context), method, clientConnection, hubClient, communicationFailedCts.Token);
          await invoker.Cleanup();

          if (ServerCapabilities.VerboseLogging)
          {
            logger.LogInformation("Completed invocation for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}", clientName, method, clientConnection.ConnectionId);
          }

          return new DirectInvocationResponse
          {
            Metadata = clientConnection.ClientCapabilities.SendHTTPMetadata ? responseMetadata : null,
            Data = responseData != null ? await responseData.ReadAllBytes(communicationFailedCts.Token) : []
          };
        }
        catch (Exception ex)
        {
          logger.LogError("Invocation error for {WebSocketClientName}, method: {Method}. Connection ID: {ConnectionId}. Error: {ErrorMessage}", clientName, method, clientConnection.ConnectionId, ex.Message);

          await communicationFailedCts.CancelAsync();
          await invoker.Cleanup();
        }
      } while (!wasSuccessfulInvocation && ServerCapabilities.FaultToleranceEnabled && retryCount++ < ServerCapabilities.MaxRetryAttempts);

      throw new HubException("Invocation failed after multiple attempts");
    }
    #endregion

    // TO DO: Refactor this to a hub filter
    private async Task ConnectionChecks(string id, string name, string? clientId)
    {
      if (string.IsNullOrWhiteSpace(clientId))
      {
        logger.LogWarning("Client method invocation attempt without client identity header. Connection ID: {ConnectionId}", Context.ConnectionId);
        Context.Abort();
        return;
      }

      var clientConectionExists = webSocketClientConnectionsService.ExistsWebSocketClientConnection(id, name, clientId);

      if (!clientConectionExists)
      {
        Context.Abort();
      }
    }

    private (string, string, string, string?, string?) GetClientInfoFromContext()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      var email = Context.User!.Email();
      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();
      var clientId = Context.GetHttpContext()?.Request.Headers["X-Client-Identity"].FirstOrDefault();

      return (id, name, email, clientToken, clientId);
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

    private static HttpRequestMetadata ProcessMetadata(DirectInvocationData requestData, HubCallerContext context)
    {
      var user = new HttpUser
      {
        Identifier = context.User!.Identifier(),
        Role = UserTypes.Client
      };

      if (requestData.Metadata != null)
      {
        requestData.Metadata.User = user;
        return requestData.Metadata;
      }

      return new HttpRequestMetadata
      {
        User = user,
      };
    }
  }
}