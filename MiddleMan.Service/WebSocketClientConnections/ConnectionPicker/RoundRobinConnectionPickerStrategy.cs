using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Service.WebSocketClientConnections.ConnectionPicker
{
  class RoundRobinConnectionPickerStrategy : IConnectionPickerStrategy
  {
    public int PickAndUpdate(ClientConnections clientConnections)
    {
      if (clientConnections.ConnectionIds.Count == 0)
        throw new InvalidOperationException("No connections available to pick.");

      int lastPickedIndex = clientConnections.Metadata?.LastPickedIndex ?? 0;
      int indexToPick = (lastPickedIndex + 1) % clientConnections.ConnectionIds.Count;

      // Update the last picked index for the next round
      clientConnections.Metadata ??= new ClientConnectionsMetadata();
      clientConnections.Metadata.LastPickedIndex = indexToPick;

      return indexToPick;
    }
  }
}