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
  }
}