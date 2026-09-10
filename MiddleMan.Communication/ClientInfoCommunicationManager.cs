using MiddleMan.Communication.Channels;
using MiddleMan.Communication.Constants;
using MiddleMan.Communication.Messages;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Communication
{
  public class ClientInfoCommunicationManager(ICommunicationChannel communicationChannel, IWebSocketClientConnectionsService webSocketClientConnectionsService)
  {
    private readonly ICommunicationChannel communicationChannel = communicationChannel;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;

    public async Task StartListening(string identifier, string name)
    {
      await communicationChannel.SubscribeRequestAsync<ClientQuery, ClientConnection>(
        ClientInfoQueryChannelKey(identifier, name),
        async query =>
        {
          if (query.Query != ClientInfoQueries.ConnectionId) return null;

          return webSocketClientConnectionsService.GetWebSocketClientConnection(identifier, name);
        });
    }

    public async Task StopListening(string identifier, string name)
    {
      await communicationChannel.UnsubscribeAsync(ClientInfoQueryChannelKey(identifier, name));
    }

    public async Task<ClientConnection?> QueryClientConnection(string identifier, string name, CancellationToken cancellationToken = default)
    {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(ServerCapabilities.ClientConnectionTimeoutSeconds));

      try
      {
        var response = await communicationChannel.RequestAsync<ClientQuery, ClientConnection>(
          ClientInfoQueryChannelKey(identifier, name),
          new ClientQuery { Query = ClientInfoQueries.ConnectionId },
          cts.Token);

        if (response == null) return null;

        response.SameServerConnection = false;
        return response;
      }
      catch (OperationCanceledException) when (cts.IsCancellationRequested)
      {
        return null;
      }
    }

    private static string ClientInfoQueryChannelKey(string identifier, string name) => $"client-info-query:{identifier}:{name}";
  }
}