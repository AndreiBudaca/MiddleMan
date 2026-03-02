using MiddleMan.Data.InMemory;

namespace MiddleMan.Web.Communication.Adapters;

public class RedisBoundedListAdapter(string session, ISharedInMemoryContext sharedInMemoryContext) : IDataWriterAdapter
{
  private readonly ISharedInMemoryContext sharedInMemoryContext = sharedInMemoryContext;
  private readonly string session = session;

  public async IAsyncEnumerable<byte[]> Adapt()
  {
    byte[]? bytes;

    do
    {
      bytes = await sharedInMemoryContext.GetRawBytesFromBoundedList(session);
      if (bytes != null && bytes.Length > 0)
      {
        yield return bytes;
      }
    } while (bytes != null && bytes.Length > 0);

    await sharedInMemoryContext.TerminateBoundedList(session);
  }
}
