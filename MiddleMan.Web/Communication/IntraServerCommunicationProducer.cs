using MiddleMan.Core;
using MiddleMan.Data.InMemory;
using MiddleMan.Web.Communication.Adapters;

namespace MiddleMan.Web.Communication;

public class IntraServerCommunicationProducer(ISharedInMemoryContext sharedInMemoryContext)
{
  private readonly ISharedInMemoryContext sharedInMemoryContext = sharedInMemoryContext;

  public Task WriteAsync(IDataWriterAdapter dataWriterAdapter, string correlation)
  {
    return WriteAsync(dataWriterAdapter.Adapt(), correlation);
  }

  public async Task WriteAsync(IAsyncEnumerable<byte[]> dataSource, string correlation)
  {
    await sharedInMemoryContext.CreateBoundedList(correlation, ServerCapabilities.IntraServerBufferedChunks);

    await foreach (var bytes in dataSource)
    {
      await sharedInMemoryContext.AddRawBytesToBoundedList(correlation, bytes);
    }

    await sharedInMemoryContext.AddRawBytesToBoundedList(correlation, []);
  }
}
