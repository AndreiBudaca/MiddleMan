using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClients;
using MiddleMan.Service.WebSocketClients.Dto;
using MiddleMan.Web.Infrastructure.Identity;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub(IWebSocketClientsService webSocketClientsService) : Hub
  {
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;

    public async Task AddMethodInfo(List<WebSocketClientMethodDto> methods)
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      if (!await webSocketClientsService.ExistsWebSocketClient(id, name)) return;

      await webSocketClientsService.AddWebSocketClient(id, name, new WebSocketClientDataDto
      {
        ConnectionId = Context.ConnectionId,
        Methods = methods
      });
    }

    public override async Task OnConnectedAsync()
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      if (await webSocketClientsService.ExistsWebSocketClient(id, name))
      {
        throw new HubException($"A second client with the same name tried to connect. ID = {id}, Name = {name}");
      }

      await webSocketClientsService.AddWebSocketClient(id, name, new WebSocketClientDataDto
      {
        ConnectionId = Context.ConnectionId,
      });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
      var id = Context.User?.Identifier() ?? string.Empty;
      var name = Context.User?.Name() ?? string.Empty;

      await webSocketClientsService.DeleteWebSocketClient(id, name);
    }
  }
}
