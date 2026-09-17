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

    public Task StartListening(string identifier, string name)
    {
      var key = ClientInfoQueryChannelKey(identifier, name);
      return ClientInfoSubscriptionRegistry.GetOrAddAsync(
        key,
        () => communicationChannel.SubscribeRequestAsync<ClientQuery, ClientConnection>(
          key,
          query => HandleQueryAsync(query, identifier, name)));
    }

    public Task StopListening(string identifier, string name)
    {
      return ClientInfoSubscriptionRegistry.RemoveAndDisposeAsync(ClientInfoQueryChannelKey(identifier, name));
    }

    private ClientConnection? HandleQueryAsync(ClientQuery query, string identifier, string name)
    {
      if (query.Query != ClientInfoQueries.ConnectionId) return null;

      return webSocketClientConnectionsService.GetWebSocketClientConnection(identifier, name);
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

    private static string ClientInfoQueryChannelKey(string identifier, string name) => $"client-info-query.{identifier}.{name}";
  }
}
