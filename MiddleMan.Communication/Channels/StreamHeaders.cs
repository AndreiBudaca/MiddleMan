using NATS.Client.Core;

namespace MiddleMan.Communication.Channels
{
  internal static class StreamHeaders
  {
    public const string KindHeader = "MM-Kind";
    public const string SeqHeader = "MM-Seq";
    public const string FinalHeader = "MM-Final";

    public const string DataKind = "data";
    public const string EndKind = "end";
    public const string ErrorKind = "error";

    public static NatsHeaders Data(long seq) => new()
    {
      { KindHeader, DataKind },
      { SeqHeader, seq.ToString() },
    };

    public static NatsHeaders End(long finalSeq) => new()
    {
      { KindHeader, EndKind },
      { FinalHeader, finalSeq.ToString() },
    };

    public static NatsHeaders Error() => new()
    {
      { KindHeader, ErrorKind },
    };

    public readonly record struct Parsed(string Kind, long Seq, long FinalSeq);

    public static bool TryParse(NatsHeaders? headers, out Parsed parsed)
    {
      parsed = default;

      if (headers == null || !headers.TryGetValue(KindHeader, out var kindValues))
      {
        return false;
      }

      var kind = kindValues.ToString();
      if (kind == DataKind)
      {
        if (!headers.TryGetValue(SeqHeader, out var seqValues) || !long.TryParse(seqValues.ToString(), out var seq) || seq < 0)
        {
          return false;
        }
        parsed = new Parsed(kind, seq, 0);
        return true;
      }

      if (kind == EndKind)
      {
        if (!headers.TryGetValue(FinalHeader, out var finalValues) || !long.TryParse(finalValues.ToString(), out var finalSeq) || finalSeq < 0)
        {
          return false;
        }
        parsed = new Parsed(kind, 0, finalSeq);
        return true;
      }

      if (kind == ErrorKind)
      {
        parsed = new Parsed(kind, 0, 0);
        return true;
      }

      return false;
    }
  }
}
