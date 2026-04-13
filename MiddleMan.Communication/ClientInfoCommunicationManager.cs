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
      await communicationChannel.SubscribeAsync(ClientInfoQueryChannelKey(identifier, name), async (ClientQuery query) =>
      {
        switch (query.Query)
        {
          case ClientInfoQueries.ConnectionId:
            var clientConnection = webSocketClientConnectionsService.GetWebSocketClientConnection(identifier, name);
            if (clientConnection != null)
            {
              await communicationChannel.PublishAsync(ClientInfoConnectionResponseChannelKey(identifier, name, query.RespondTo), clientConnection);
            }
            break;
        }
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
        var queryKey = Guid.NewGuid().ToString();
        var responseTask = await communicationChannel.PeekChannelAsync<ClientConnection>(ClientInfoConnectionResponseChannelKey(identifier, name, queryKey), cts.Token);
        await communicationChannel.PublishAsync(ClientInfoQueryChannelKey(identifier, name), new ClientQuery { Query = ClientInfoQueries.ConnectionId, RespondTo = queryKey });

        var response = await responseTask;
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
    private static string ClientInfoConnectionResponseChannelKey(string identifier, string name, string queryKey) => $"client-info-connection-response:{identifier}:{name}:{queryKey}";
  }
}