using System.Collections.Concurrent;
using System.Threading.Channels;
using MiddleMan.Core;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.CommunicationSync.SameServer
{
  public static class CommunicationSessionRegistry
  {
    private static readonly ConcurrentDictionary<Guid, CommunicationControl> sessions = new();

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

      sessions[correlation] = control;

      return control;
    }

    public static CommunicationSession? GetSession(Guid correlation)
    {
      sessions.TryGetValue(correlation, out var session);
      return ToSession(session);
    }

    public static void RemoveSession(Guid correlation, bool cancel = false)
    {
      bool exists = sessions.Remove(correlation, out var session);

      if (!exists || !cancel) return;

      // Cancel any pending tasks and complete the channel to signal the end of the session
      session?.TryCancelAll();
    }

    private static CommunicationSession? ToSession(CommunicationControl? control)
    {
      if (control == null) return null;

      return new CommunicationSession
      {
        ChannelWriterSource = control.ChannelWriterSource,
        WriterCompletion = control.WriterCompletionSource.Task,
        ChannelReaderSource = control.ChannelReaderSource,
        ReaderCompletion = control.ReaderCompletionSource.Task,
        ResponseMetadataSource = control.ResponseMetadataSource
      };
    }
  }
}