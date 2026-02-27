using MiddleMan.Core;

namespace MiddleMan.Service.WebSocketClientConnections.Classes
{
  public class ClientCapabilities : IEquatable<ClientCapabilities>
  {
    public int Version { get; set; } = ServerCapabilities.AllowedVersions[0];

    public bool SupportsStreaming { get; set; }

    public bool SendHTTPMetadata { get; set; }

    public bool Equals(ClientCapabilities? other)
    {
      if (other == null) return false;
      return Version == other.Version &&
        SupportsStreaming == other.SupportsStreaming &&
        SendHTTPMetadata == other.SendHTTPMetadata;
    }

    public override bool Equals(object? obj)
    {
      return Equals(obj as ClientCapabilities);
    }

    public override int GetHashCode()
    {
      throw new NotImplementedException();
    }
  }
}