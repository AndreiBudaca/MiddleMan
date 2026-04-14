using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Hubs.Models;
using MiddleMan.Web.Infrastructure.Identity;
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
      var (id, name, clientToken, clientId) = GetClientInfoFromContext();

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
      var (id, name, _, clientId) = GetClientInfoFromContext();

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
      var (id, name, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task MethodsRaw(byte[] methodsBytes)
    {
      var (id, name, _, clientId) = GetClientInfoFromContext();

      await ConnectionChecks(id, name, clientId);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, methodsBytes, CancellationToken.None);
    }

    public async Task<ServerInfoModel> Negociate(ClientInfoModel clientInfo)
    {
      var (id, name, _, clientId) = GetClientInfoFromContext();
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
        MaxMessageLength = ServerCapabilities.MaxContentLength,
        MethodSignature = client?.Signature,
      };
    }
    #endregion

    #region [Communication methods]
    public async Task AddReadChannel(Guid correlation, ChannelReader<byte[]> channelReader)
    {
      var (id, name, _, clientId) = GetClientInfoFromContext();
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
      var (id, name, _, clientId) = GetClientInfoFromContext();
      await ConnectionChecks(id, name, clientId);

      var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1));
      await communicationManager.RegisterSessionWriterChannelAsync(channel.Writer, correlation);

      return channel.Reader;
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

    private (string, string, string?, string?) GetClientInfoFromContext()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();
      var clientId = Context.GetHttpContext()?.Request.Headers["X-Client-Identity"].FirstOrDefault();

      return (id, name, clientToken, clientId);
    }
  }
}
