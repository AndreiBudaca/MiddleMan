using MiddleMan.Core;

namespace MiddleMan.Service.WebSocketClientMethods
{
  public interface IWebSocketClientMethodService
  {
    Task ReceiveMethodsAsync(string identifier, string name, IAsyncEnumerable<DataChunk> methods, CancellationToken cancellationToken);
    Task ReceiveMethodsAsync(string identifier, string name, byte[] methods, CancellationToken cancellationToken);
  }
}
