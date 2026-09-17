namespace MiddleMan.Communication.Channels
{
  public sealed class StreamEnvelope
  {
    public const string DataKind = "data";
    public const string EndKind = "end";
    public const string ErrorKind = "error";

    public string Kind { get; set; } = DataKind;
    public byte[]? Payload { get; set; }
    public int Length { get; set; }
    public string? Error { get; set; }

    public static StreamEnvelope Data(byte[] payload, int length) => new()
    {
      Kind = DataKind,
      Payload = payload,
      Length = length,
    };

    public static StreamEnvelope End() => new() { Kind = EndKind };

    public static StreamEnvelope FromError(string message) => new() { Kind = ErrorKind, Error = message };
  }
}
