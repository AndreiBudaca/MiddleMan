using MiddleMan.Data.InMemory;
using MiddleMan.Web.Communication.Adapters;
using MiddleMan.Web.Communication.SyncMechanisms;
using System.Threading.Channels;

namespace MiddleMan.Web.Communication
{
  public class CommunicationManager(IInMemoryContext context)
  {
    private readonly IInMemoryContext context = context;

    private readonly SessionAsyncMonitorPool sessionReadMonitor = new();
    private readonly SessionAsyncMonitorPool sessionReadEndedMonitor = new();

    private readonly SessionAsyncMonitorPool sessionWriteMonitor = new();
    private readonly SessionAsyncMonitorPool sessionWriteEndedMonitor = new();

    public async Task RegisterSessionReaderChannelAsync(ChannelReader<byte[]> channelReader, Guid correlation)
    {
      await sessionReadMonitor.SetResourceAndNotify
      (
        async () => await context.AddToHash("readers", correlation.ToString(), channelReader),
        correlation
      );

      _ = await sessionReadEndedMonitor.WaitToGetResource
      (
        async () => await context.GetFromHash<ChannelReader<byte[]>>("readers", correlation.ToString()),
        (reader) => reader == null,
        correlation
      );
    }

    public async Task RegisterSessionWriterChannelAsync(ChannelWriter<byte[]> channelWriter, Guid correlation)
    {
      await sessionWriteMonitor.SetResourceAndNotify
      (
        async () => await context.AddToHash("writers", correlation.ToString(), channelWriter),
        correlation
      );

      _ = await sessionWriteEndedMonitor.WaitToGetResource
      (
        async () => await context.GetFromHash<ChannelWriter<byte[]>>("writers", correlation.ToString()),
        (writer) => writer == null,
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
        async () => await context.GetFromHash<ChannelWriter<byte[]>>("writers", correlation.ToString()),
        (writer) => writer != null,
        correlation
      );

      if (writer != null)
      {
        await foreach (var chunk in dataSource)
        {
          await writer.WriteAsync(chunk);
        }
        writer.Complete();
      }

      await sessionWriteEndedMonitor.SetResourceAndNotify
      (
        async () => await context.RemoveFromHash("writers", correlation.ToString()),
        correlation
      );
    }

    public async IAsyncEnumerable<byte[]> ReadAsync(Guid correlation)
    {
      var reader = await sessionReadMonitor.WaitToGetResource
      (
        async () => await context.GetFromHash<ChannelReader<byte[]>>("readers", correlation.ToString()),
        (reader) => reader != null,
        correlation
      );

      if (reader == null)
      {
        yield break;
      }
      else
      {
        await foreach( var chunk in reader.ReadAllAsync())
        {
          yield return chunk; 
        }
      }

      await sessionReadEndedMonitor.SetResourceAndNotify
      (
        async () => await context.RemoveFromHash("readers", correlation.ToString()),
        correlation
      );
    }
  }
}
