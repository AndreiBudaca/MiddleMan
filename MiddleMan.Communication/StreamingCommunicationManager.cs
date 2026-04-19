using MiddleMan.Data.InMemory;
using MiddleMan.Communication.Adapters;
using MiddleMan.Communication.SyncMechanisms;
using System.Threading.Channels;
using MiddleMan.Core;
using System.Runtime.CompilerServices;

namespace MiddleMan.Communication
{
  public class StreamingCommunicationManager(IInMemoryContext context)
  {
    private readonly IInMemoryContext context = context;

    private static readonly AsyncResourceMonitor<Guid> sessionReadMonitor = new();
    private static readonly AsyncResourceMonitor<Guid> sessionReadEndedMonitor = new();

    private static readonly AsyncResourceMonitor<Guid> sessionWriteMonitor = new();

    public async Task RegisterSessionReaderChannelAsync(ChannelReader<byte[]> channelReader,
     Guid correlation, bool waitForReadToFinish = true)
    {
      await sessionReadMonitor.SetResourceAndNotify
      (
        async () => context.AddToHash("readers", correlation.ToString(), channelReader),
        correlation
      );

      if (waitForReadToFinish)
      {
        _ = await sessionReadEndedMonitor.WaitToGetResource
        (
          async () => context.GetFromHash<ChannelReader<byte[]>>("readers", correlation.ToString()),
          (reader) => reader == null,
          correlation
        );
      }
    }

    public async Task RegisterSessionWriterChannelAsync(ChannelWriter<byte[]> channelWriter, Guid correlation)
    {
      await sessionWriteMonitor.SetResourceAndNotify
      (
        async () => context.AddToHash("writers", correlation.ToString(), channelWriter),
        correlation
      );
    }

    public Task WriteAsync(IDataWriterAdaptor adapter, Guid correlation, CancellationToken cancellationToken = default)
    {
      return WriteAsync(adapter.Adapt(), correlation, cancellationToken);
    }

    public async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation, CancellationToken cancellationToken = default)
    {
      ChannelWriter<byte[]>? writer;

      using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
      {
        cts.CancelAfter(TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));

        writer = await sessionWriteMonitor.WaitToGetResource
        (
          async () => context.GetFromHash<ChannelWriter<byte[]>>("writers", correlation.ToString()),
          (writer) => writer != null,
          correlation,
          cts.Token
        );
      }

      if (writer != null)
      {
        try
        {
          await foreach (var chunk in dataSource.WithCancellation(cancellationToken))
          {
            var buff = new byte[chunk.Length];
            Buffer.BlockCopy(chunk, 0, buff, 0, chunk.Length);

            await writer.WriteAsync(buff, cancellationToken);
          }
        }
        finally
        {
          writer.Complete();
        }
      }

      context.RemoveFromHash("writers", correlation.ToString());
    }

    public async IAsyncEnumerable<byte[]> ReadAsync(Guid correlation, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      ChannelReader<byte[]>? reader;

      using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
      {
        cts.CancelAfter(TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));

        reader = await sessionReadMonitor.WaitToGetResource
        (
          async () => context.GetFromHash<ChannelReader<byte[]>>("readers", correlation.ToString()),
          (reader) => reader != null,
          correlation,
          cts.Token
        );
      }

      if (reader == null)
      {
        yield break;
      }
      else
      {
        await foreach (var chunk in reader.ReadAllAsync(cancellationToken))
        {
          if (chunk == null) throw new InvalidDataException("Received null chunk from reader.");
          
          yield return chunk;
        }
      }

      await sessionReadEndedMonitor.SetResourceAndNotify
      (
        async () => context.RemoveFromHash("readers", correlation.ToString()),
        correlation
      );
    }
  }
}
