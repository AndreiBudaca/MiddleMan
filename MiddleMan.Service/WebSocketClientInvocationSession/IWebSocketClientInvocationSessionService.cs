using MiddleMan.Service.WebSocketClientInvocationSession.Enums;

namespace MiddleMan.Service.WebSocketClientInvocationSession
{
  public interface IWebSocketClientInvocationSessionService
  {
    Task RegisterSession(Guid correlation);
    Task ClearSession(Guid correlation);
    Task<bool> ExistsSession(Guid correlation);
    Task<byte[]?> GetDistributedSessionData(Guid correlation, SessionDataTypes dataType);
    Task SendSessionData(Guid correlation, byte[] data, SessionDataTypes dataType);
    Task CompleteSessionData(Guid correlation, SessionDataTypes dataType);
  }
}