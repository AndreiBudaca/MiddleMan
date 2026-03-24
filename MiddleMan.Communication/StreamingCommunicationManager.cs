using MiddleMan.Data.InMemory;
using MiddleMan.Communication.Adapters;
using MiddleMan.Communication.SyncMechanisms;
using System.Threading.Channels;

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

    public Task WriteAsync(IDataWriterAdapter adapter, Guid correlation)
    {
      return WriteAsync(adapter.Adapt(), correlation);
    }

    public async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, Guid correlation)
    {
      var writer = await sessionWriteMonitor.WaitToGetResource
      (
        async () => context.GetFromHash<ChannelWriter<byte[]>>("writers", correlation.ToString()),
        (writer) => writer != null,
        correlation
      );

      if (writer != null)
      {
        try
        {
          await foreach (var chunk in dataSource)
          {
            var buff = new byte[chunk.Length];
            Buffer.BlockCopy(chunk, 0, buff, 0, chunk.Length);

            await writer.WriteAsync(buff);
          }
        }
        finally
        {
          writer.Complete();
        }
      }

      context.RemoveFromHash("writers", correlation.ToString());
    }

    public async IAsyncEnumerable<byte[]> ReadAsync(Guid correlation)
    {
      var reader = await sessionReadMonitor.WaitToGetResource
      (
        async () => context.GetFromHash<ChannelReader<byte[]>>("readers", correlation.ToString()),
        (reader) => reader != null,
        correlation
      );

      if (reader == null)
      {
        yield break;
      }
      else
      {
        await foreach (var chunk in reader.ReadAllAsync())
        {
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
