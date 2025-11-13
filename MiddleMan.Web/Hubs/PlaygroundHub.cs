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
    private readonly IWebSocketClientsService webSocketClientsService = webSocketClientsService;
    private readonly IWebSocketClientMethodService webSocketClientMethodService = webSocketClientMethodService;

    public override async Task OnConnectedAsync()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      var clientData = await webSocketClientsService.GetWebSocketClient(id, name);
      if (clientData == null || clientData.TokenHash == null)
      {
        Context.Abort();
        return;
      }
      
      var clientToken = Context.GetHttpContext()?.Request.Headers.Authorization.FirstOrDefault();
      if (clientToken == null || !clientToken.StartsWith("Bearer "))
      {
        Context.Abort();
        return;
      }

      var tokenHash = Convert.FromBase64String(clientToken[7..]);
      if (!tokenHash.SequenceEqual(clientData.TokenHash))
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

    public async Task Methods(ChannelReader<byte[]> channelReader)
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();
      
      await ConnectionChecks(id, name);

      await webSocketClientMethodService.ReceiveMethodsAsync(id, name, channelReader.ReadAllAsync(), CancellationToken.None);
    }

    public async Task<byte[]> Signatures()
    {
      var id = Context.User!.Identifier();
      var name = Context.User!.Name();

      await ConnectionChecks(id, name);

      return [0x00, 0x00, 0x00];
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
