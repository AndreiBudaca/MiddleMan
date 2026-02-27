using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections.ConnectionPicker
{
  public interface IConnectionPickerStrategy
  {
    public int PickAndUpdate(ClientConnections clientConnections);

    public static IConnectionPickerStrategy Default => new RoundRobinConnectionPickerStrategy();
  }
}