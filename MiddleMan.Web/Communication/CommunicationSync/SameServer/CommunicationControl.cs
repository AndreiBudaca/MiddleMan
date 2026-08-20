using System.Threading.Channels;
using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.CommunicationSync.SameServer
{
  public class CommunicationControl
  {
    public required TaskCompletionSource<ChannelWriter<DataChunk>> ChannelWriterSource { get; init; }

    public required TaskCompletionSource WriterCompletionSource { get; init; }

    public required TaskCompletionSource<ChannelReader<DataChunk>> ChannelReaderSource { get; init; }

    public required TaskCompletionSource<HttpResponseMetadata?> ResponseMetadataSource { get; init; }

    public required TaskCompletionSource ReaderCompletionSource { get; init; }

    public void TryCancelAll()
    {
      ChannelWriterSource.TrySetCanceled();
      WriterCompletionSource.TrySetCanceled();
      ChannelReaderSource.TrySetCanceled();
      ReaderCompletionSource.TrySetCanceled();
      ResponseMetadataSource.TrySetCanceled();
    }
  }
}