using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace MiddleMan.Core.Extensions.Classes.Channel
{
  public class DataChunkChannelReaderAdapter(ChannelReader<byte[]> byteChannelReader) : ChannelReader<DataChunk>
  {
    private readonly ChannelReader<byte[]> byteChannelReader = byteChannelReader;

    public override bool TryRead([MaybeNullWhen(false)] out DataChunk item)
    {
      if (byteChannelReader.TryRead(out var byteArray))
      {
        item = new DataChunk(byteArray);
        return true;
      }

      item = null;
      return false;
    }

    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
      return byteChannelReader.WaitToReadAsync(cancellationToken);
    }
  }
}