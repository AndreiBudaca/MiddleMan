using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using MiddleMan.Core;
using MiddleMan.Core.Extensions;
using MiddleMan.Web.Communication.CommunicationSync.SameServer;
using MiddleMan.Web.Communication.Metadata;

namespace MiddleMan.Web.Communication.Target
{
  public class SameServerWebSocketStreamingInvocationTarget : ICommunicationTarget
  {
    private readonly Guid? correlation;
    private readonly CommunicationControl? control;
    private readonly ISingleClientProxy? clientProxy;
    private readonly string method;

    public SameServerWebSocketStreamingInvocationTarget(ISingleClientProxy clientProxy, string method)
    {
      control = null;
      correlation = null;

      this.clientProxy = clientProxy;
      this.method = method;
    }
    
    public SameServerWebSocketStreamingInvocationTarget(Guid correlation, CommunicationControl control)
    {
      this.correlation = correlation;
      this.control = control;

      clientProxy = null;
      method = string.Empty;
    }

    public async Task<ResponseData?> WriteData(RequestData? data, CancellationToken cancellationToken = default)
    {
      var communicationCorrelation = correlation ?? Guid.NewGuid();
      var communicationControl = control ?? CommunicationSessionRegistry.RegisterSession(communicationCorrelation);
      
      try
      {
        // Send the correlation ID and metadata to the client to initiate the streaming session
        if (clientProxy != null)
        {
          await clientProxy.SendAsync(method, communicationCorrelation, data?.Metadata, cancellationToken);
        }

        // Wait for the client to subscribe to the server channel
        using (var timeoutDisposable = communicationControl.ChannelWriterSource.SetTimeout(cancellationToken: cancellationToken))
        {
          var channelWriter = await communicationControl.ChannelWriterSource.Task
            ?? throw new InvalidOperationException("ChannelWriter is null.");

          await WriteDataToChannel(channelWriter, communicationControl.WriterCompletionSource, data, cancellationToken);
        }

        // Wait for the client to register its channel
        ChannelReader<DataChunk>? channelReader;
        using (var timeoutDisposable = communicationControl.ChannelReaderSource.SetTimeout(cancellationToken: cancellationToken))
        {
          channelReader = await communicationControl.ChannelReaderSource.Task
            ?? throw new InvalidOperationException("ChannelReader is null.");
        }

        // Wait for the client to send the response metadata
        HttpResponseMetadata? responseMetadata;
        using (var timeoutDisposable = communicationControl.ResponseMetadataSource.SetTimeout(cancellationToken: cancellationToken))
        {
          responseMetadata = await communicationControl.ResponseMetadataSource.Task;
        }

        CommunicationSessionRegistry.RemoveSession(communicationCorrelation);

        return new ResponseData
        {
          Metadata = responseMetadata,
          Data = ReadDataFromChannel(channelReader, communicationControl.ReaderCompletionSource, cancellationToken)
        };
      }
      catch
      {
        CommunicationSessionRegistry.RemoveSession(communicationCorrelation, true);
        throw;
      }
    }

    private async static Task WriteDataToChannel(ChannelWriter<DataChunk> channelWriter, TaskCompletionSource writerCompletionSource, RequestData? data, CancellationToken cancellationToken)
    {
      if (data?.Data == null)
      {
        channelWriter.Complete();
        return;
      }

      try
      {
        await foreach (var chunk in data.Data.WithCancellation(cancellationToken))
        {
          await channelWriter.WriteAsync(chunk, cancellationToken);
        }
      }
      finally
      {
        channelWriter.Complete();
        _ = writerCompletionSource.TrySetResult();
      }
    }

    private static async IAsyncEnumerable<DataChunk> ReadDataFromChannel(ChannelReader<DataChunk> channelReader, TaskCompletionSource readerCompletionSource, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      try
      {
        await foreach (var chunk in channelReader.ReadAllAsync(cancellationToken))
        {
          yield return chunk;
        }
      }
      finally
      {
        _ = readerCompletionSource.TrySetResult();
      }
    }
  }
}