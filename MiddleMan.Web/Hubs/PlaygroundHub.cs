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
    IWebSocketClientConnectionsService webSocketClientConnectionsService) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly StreamingCommunicationManager communicationManager = communicationManager;

    #region [Clinet connection hooks]
    public override async Task OnConnectedAsync()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();

      var isAuthorized = await AuthorizeClient(id, name, clientToken);
      if (!isAuthorized)
      {
        Context.Abort();
        return;
      }

      var onServerConnectionCount = webSocketClientConnectionsService.AddWebSocketClientConnection(id, name, Context.ConnectionId);
      if (onServerConnectionCount == 1)
      {
        await clientInfoCommunicationManager.StartListening(id, name);
      }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      var onServerConnectionCount = webSocketClientConnectionsService.DeleteWebSocketClientConnection(id, name, Context.ConnectionId);
      if (onServerConnectionCount == 0)
      {
        await clientInfoCommunicationManager.StopListening(id, name);
      }
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
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await ConnectionChecks(id, name);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task<ServerInfoModel> Negociate(ClientInfoModel clientInfo)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      await ConnectionChecks(id, name);

      if (clientInfo == null) return new ServerInfoModel { IsAccepted = false };
      if (!ServerCapabilities.AllowedVersions.Contains(clientInfo.Version)) return new ServerInfoModel { IsAccepted = false };

      webSocketClientConnectionsService.AddWebSockerClientConnectionCapabilities(
        id,
        name,
        Context.ConnectionId,
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
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      await ConnectionChecks(id, name);

      var sameServerInvocation = intraServerCommunicationManager.ExistsRequestSession(correlation);
      await communicationManager.RegisterSessionReaderChannelAsync(channelReader, correlation, sameServerInvocation);

      if (!sameServerInvocation)
      {
        try
        {
          await intraServerCommunicationManager.RegisterResponseSession(correlation, false);

          await communicationManager.WriteAsync(intraServerCommunicationManager.ReadRequestAsync(correlation), correlation);
          await intraServerCommunicationManager.WriteResponseAsync(communicationManager.ReadAsync(correlation), correlation);
        }
        finally
        {
          await intraServerCommunicationManager.ClearResponseSession(correlation, false);
        }
      }
    }

    public async Task<ChannelReader<byte[]>> SubscribeToServer(Guid correlation)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      await ConnectionChecks(id, name);

      var channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1));
      await communicationManager.RegisterSessionWriterChannelAsync(channel.Writer, correlation);

      return channel.Reader;
    }
    #endregion

    // TO DO: Refactor this to a hub filter
    private async Task ConnectionChecks(string id, string name)
    {
      var clientConectionExists = webSocketClientConnectionsService.ExistsWebSocketClientConnection(id, name);

      if (!clientConectionExists)
      {
        Context.Abort();
      }
    }
  }
}
