namespace MiddleMan.Communication.Channels
{
  /// <summary>
  /// Flow-control credit published by a stream reader to its writer.
  /// Transported through JetStream (same stream, dedicated subject) so credits
  /// are never lost regardless of reader/writer start order.
  /// </summary>
  public sealed class CreditEnvelope
  {
    public int Count { get; set; }
  }
}
