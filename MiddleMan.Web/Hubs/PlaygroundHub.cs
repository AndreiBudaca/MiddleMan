using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Hubs.Models;
using MiddleMan.Web.Infrastructure.Identity;
using System.Threading.Channels;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService,
    IWebSocketClientMethodService webSocketClientMethodService,
    StreamingCommunicationManager communicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;
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

      await webSocketClientConnectionsService.AddWebSocketClientConnection(id, name, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await webSocketClientConnectionsService.DeleteWebSocketClientConnection(id, name, Context.ConnectionId);
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

      var canAcceptClient = await webSocketClientConnectionsService.AddWebSockerClientConnectionCapabilities(id, name,
      new ClientCapabilities
      {
        Version = clientInfo.Version,
        SupportsStreaming = clientInfo.SupportsStreaming,
        SendHTTPMetadata = clientInfo.SendHTTPMetadata,
      });
      if (!canAcceptClient) return new ServerInfoModel { IsAccepted = false };

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

      await communicationManager.RegisterSessionReaderChannelAsync(channelReader, correlation);
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
      var clientConectionExists = await webSocketClientConnectionsService.ExistsWebSocketClientConnection(id, name);

      if (!clientConectionExists)
      {
        Context.Abort();
      }
    }
  }
}
