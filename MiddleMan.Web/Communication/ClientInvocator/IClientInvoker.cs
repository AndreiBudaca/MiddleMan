using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.ClientInvocator
{
  public interface IClientInvoker
  {
    public Task<(HttpResponseMetadata?, IAsyncEnumerable<byte[]>?)> Invoke(IAsyncEnumerable<byte[]> data, HttpRequestMetadata metadata, string method, ClientConnection webSocketClientConnection, ISingleClientProxy hubClient, CancellationToken cancellationToken);
    public Task Cleanup();
  }
}