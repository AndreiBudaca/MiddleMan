using Microsoft.AspNetCore.SignalR;
using MiddleMan.Communication;
using MiddleMan.Core;
using MiddleMan.Service.WebSocketClientConnections;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Target;
using MiddleMan.Web.Hubs;

namespace MiddleMan.Web.Communication.Resolver
{
  public class CommunicationTargetResolver(
    IHubContext<PlaygroundHub> hubContext,
    ClientInfoCommunicationManager clientInfoCommunicationManager,
    IWebSocketClientConnectionsService webSocketClientConnectionsService,
    IntraServerCommunicationManager intraServerCommunicationManager) : ICommunicationTargetResolver
  {
    private readonly IntraServerCommunicationManager intraServerCommunicationManager = intraServerCommunicationManager;
    private readonly ClientInfoCommunicationManager clientInfoCommunicationManager = clientInfoCommunicationManager;
    private readonly IWebSocketClientConnectionsService webSocketClientConnectionsService = webSocketClientConnectionsService;

    public async Task<ICommunicationTarget?> ResolveTarget(TargetDescription targetDescription, CancellationToken cancellationToken = default)
    {
      var clientConnection = webSocketClientConnectionsService.GetWebSocketClientConnection(targetDescription.UserId, targetDescription.Name);

      if (clientConnection == null && ServerCapabilities.ClusterMode)
      {
        clientConnection = await clientInfoCommunicationManager.QueryClientConnection(targetDescription.UserId, targetDescription.Name, cancellationToken);

        // double check after querying other servers in cluster (in case the client connection was established while we were querying other servers)
        clientConnection ??= webSocketClientConnectionsService.GetWebSocketClientConnection(targetDescription.UserId, targetDescription.Name);
      }

      return ResolveFromConnectionInfo(clientConnection, targetDescription.Method);
    }

    private ICommunicationTarget? ResolveFromConnectionInfo(ClientConnection? connection, string method)
    {
      if (connection?.ConnectionId == null) return null;

      var singleClientProxy = hubContext.Clients.Client(connection.ConnectionId);

      if (!connection.ClientCapabilities.SupportsStreaming) return new DirectInvocationTarget(singleClientProxy, method);
      if (connection.SameServerConnection) return new SameServerWebSocketStreamingInvocationTarget(singleClientProxy, method);
      return new MultiServerWebSocketStreamingInvocationTarget(intraServerCommunicationManager, singleClientProxy, method);
    }
  }
}