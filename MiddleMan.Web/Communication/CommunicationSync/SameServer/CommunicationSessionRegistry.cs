using System.Collections.Concurrent;
using System.Threading.Channels;
using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.CommunicationSync.SameServer
{
  public static class CommunicationSessionRegistry
  {
    private static readonly ConcurrentDictionary<Guid, CommunicationSession> sessions = new();

    public static CommunicationControl RegisterSession(Guid correlation)
    {
      var control = new CommunicationControl
      {
        ChannelWriterSource = new TaskCompletionSource<ChannelWriter<DataChunk>>(),
        WriterCompletionSource = new TaskCompletionSource(),
        ChannelReaderSource = new TaskCompletionSource<ChannelReader<DataChunk>>(),
        ReaderCompletionSource = new TaskCompletionSource(),
        ResponseMetadataSource = new TaskCompletionSource<HttpResponseMetadata?>()
      };

      var session = new CommunicationSession
      {
        ChannelWriterSource = control.ChannelWriterSource,
        WriterCompletion = control.WriterCompletionSource.Task,
        ChannelReaderSource = control.ChannelReaderSource,
        ReaderCompletion = control.ReaderCompletionSource.Task,
        ResponseMetadataSource = control.ResponseMetadataSource
      };

      sessions[correlation] = session;

      return control;
    }

    public static CommunicationSession? GetSession(Guid correlation)
    {
      sessions.TryGetValue(correlation, out var session);
      return session;
    }

    public static void RemoveSession(Guid correlation)
    {
      sessions.Remove(correlation, out var _);
    }
  }
}