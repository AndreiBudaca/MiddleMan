namespace MiddleMan.Service.WebSocketClientMethods
{
  public interface IWebSocketClientMethodService
  {
    Task ReceiveMethodsAsync(string identifier, string name, IAsyncEnumerable<byte[]> methods, CancellationToken cancellationToken);
    Task ReceiveMethodsAsync(string identifier, string name, byte[] methods, CancellationToken cancellationToken);
  }
}
