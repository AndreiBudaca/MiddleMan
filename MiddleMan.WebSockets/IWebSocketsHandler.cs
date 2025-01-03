using MiddleMan.WebSockets.Model;

namespace MiddleMan.WebSockets
{
  public interface IWebSocketsHandler
  {
    Task Accept(WebSocketData data);
    bool Close(int webSocketId);
    Task<string> CommunicateAsync(int webSocketId, string message, CancellationToken cancellationToken);
  }
}