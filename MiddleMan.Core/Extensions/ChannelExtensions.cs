using System.Runtime.CompilerServices;
using System.Threading.Channels;
using MiddleMan.Core.Extensions.Classes.Channel;

namespace MiddleMan.Core.Extensions
{
  public static class ChannelExtentions
  {
    public static ChannelReader<DataChunk> AsDataChunkChannelReader(this ChannelReader<byte[]> channel)
    {
      return new DataChunkChannelReaderAdapter(channel);
    }

    public static async IAsyncEnumerable<Memory<byte>> ReadAllAsMemory(this ChannelReader<DataChunk> channelReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      // TO DO: add a timeout for the channel reading to avoid hanging indefinitely if data isn't received
      await foreach (var chunk in channelReader.ReadAllAsync(cancellationToken))
      {
        yield return chunk.Data.AsMemory(0, chunk.Length);
      }
    }
  }
}