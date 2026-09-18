namespace MiddleMan.Communication.Channels.Classes
{
  internal readonly record struct StreamMessageOutcome(StreamMessageDisposition Disposition, byte[]? Payload)
  {
    public static StreamMessageOutcome Skipped => new(StreamMessageDisposition.Skipped, null);
    public static StreamMessageOutcome Ended => new(StreamMessageDisposition.EndOfStream, null);
    public static StreamMessageOutcome Data(byte[]? payload) => new(StreamMessageDisposition.YieldData, payload);
  }
}
