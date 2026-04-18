using Microsoft.AspNetCore.SignalR;
using MiddleMan.Service.WebSocketClientConnections.Classes;
using MiddleMan.Web.Controllers.ActionResults;

namespace MiddleMan.Web.Communication.ClientInvocator
{
  public interface IClientInvoker
  {
    public Task<IControllerResult> Invoke(HttpContext httpContext, string method, ClientConnection webSocketClientConnection, ISingleClientProxy hubClient, CancellationToken cancellationToken);
    public Task Cleanup();
  }
}