using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Service.WebSocketClients.Dto;
using MiddleMan.Web.Communication;
using MiddleMan.Web.Hubs.Models;
using MiddleMan.Web.Infrastructure.Identity;
using System.Threading.Channels;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService,
    IWebSocketClientMethodService webSocketClientMethodService,
    CommunicationManager communicationManager
    ) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;
    private readonly CommunicationManager communicationManager = communicationManager;

    public override async Task OnConnectedAsync()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();
      if (clientToken == null || !clientToken.StartsWith("Bearer "))
      {
        Context.Abort();
        return;
      }

      var isTokenValid = await webSocketClientsService.IsValidWebSocketClientToken(id, name, clientToken[7..]);
      if (!isTokenValid)
      {
        Context.Abort();
        return;
      }

      var existingClientConnection = await webSocketClientsService.GetWebSocketClientConnection(id, name);

      if (existingClientConnection is not null)
      {
        throw new HubException($"A second client with the same name tried to connect. ID = {id}, Name = {name}");
      }

      await webSocketClientsService.AddWebSocketClientConnection(id, name, new WebSocketClientConnectionDataDto
      {
        ConnectionId = Context.ConnectionId,
      });
    }

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

    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await ConnectionChecks(id, name);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task<ServerInfoModel> ServerInfo()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await ConnectionChecks(id, name);

      var client = await webSocketClientsService.GetWebSocketClient(id, name);
      return new ServerInfoModel
      {
        MaxMessageLength = ServerCapabilities.MaxContentLength,
        MethodSignature = client?.Signature,
        AcceptedVersions = ServerCapabilities.AllowedVersions
      };
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await webSocketClientsService.DeleteWebSocketClientConnection(id, name);
    }

    private async Task ConnectionChecks(string id, string name)
    {
      var clientConnection = await webSocketClientsService.GetWebSocketClientConnection(id, name);

      if (string.IsNullOrEmpty(clientConnection?.ConnectionId))
      {
        Context.Abort();
      }
    }
  }
}
