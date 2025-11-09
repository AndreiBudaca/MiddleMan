using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClientMethods;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Service.WebSocketClients.Dto;
using MiddleMan.Web.Infrastructure.Identity;
using System.Threading.Channels;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService,
    IWebSocketClientMethodService webSocketClientMethodService
    ) : Hub
  {
    private readonly IWebSocketClientsService _webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientMethodService _webSocketClientMethodService = webSocketClientMethodService;

    public override async Task OnConnectedAsync()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      var existingClientConnection = await _webSocketClientsService.GetWebSocketClientConnection(id, name);

      if (existingClientConnection is not null)
      {
        throw new HubException($"A second client with the same name tried to connect. ID = {id}, Name = {name}");
      }

      await _webSocketClientsService.AddWebSocketClient(id, name, new WebSocketClientConnectionDataDto
      {
        ConnectionId = Context.ConnectionId,
      });
    }

    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await _webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public byte[] Signatures()
    {
      return [0x00, 0x00, 0x00];
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await _webSocketClientsService.DeleteWebSocketClient(id, name);
    }
  }
}
