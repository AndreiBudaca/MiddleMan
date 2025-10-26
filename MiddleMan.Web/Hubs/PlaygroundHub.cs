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

    public async Task AddMethodInfo(List<WebSocketClientMethodDto> methods)
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      if (!await _webSocketClientsService.ExistsWebSocketClient(id, name)) return;

      await _webSocketClientsService.AddWebSocketClient(id, name, new WebSocketClientDataDto
      {
        ConnectionId = Context.ConnectionId,
        Methods = methods
      });
    }

    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      await _webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task<byte[]> Signatures()
    {
      return [0x00, 0x00, 0x00];
    }

    public override async Task OnConnectedAsync()
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      if (await _webSocketClientsService.ExistsWebSocketClient(id, name))
      {
        throw new HubException($"A second client with the same name tried to connect. ID = {id}, Name = {name}");
      }

      await _webSocketClientsService.AddWebSocketClient(id, name, new WebSocketClientDataDto
      {
        ConnectionId = Context.ConnectionId,
      });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      await _webSocketClientsService.DeleteWebSocketClient(id, name);
    }
  }
}
