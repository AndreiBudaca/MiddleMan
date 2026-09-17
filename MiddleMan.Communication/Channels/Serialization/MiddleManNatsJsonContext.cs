using System.Text.Json.Serialization;
using MiddleMan.Communication.Messages;
using MiddleMan.Service.WebSocketClientConnections.Classes;

namespace MiddleMan.Communication.Channels.Serialization
{
  [JsonSerializable(typeof(ClientQuery))]
  [JsonSerializable(typeof(ClientConnection))]
  [JsonSerializable(typeof(ClientCapabilities))]
  public sealed partial class MiddleManNatsJsonContext : JsonSerializerContext
  {
  }
}
