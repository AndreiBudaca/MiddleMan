using System.Threading.Channels;
using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.CommunicationSync.SameServer
{
  public class CommunicationSession
  {
    public required TaskCompletionSource<ChannelWriter<DataChunk>> ChannelWriterSource { get; init; }

    public required Task WriterCompletion { get; init; }

    public required TaskCompletionSource<ChannelReader<DataChunk>> ChannelReaderSource { get; init; }

    public required TaskCompletionSource<HttpResponseMetadata?> ResponseMetadataSource { get; init; }

    public required Task ReaderCompletion { get; init; }
  }
}