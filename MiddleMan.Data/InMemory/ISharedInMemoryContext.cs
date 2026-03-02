namespace MiddleMan.Data.InMemory
{
  public interface ISharedInMemoryContext : IInMemoryContext
  {
    Task CreateBoundedList(string key, int maxCount);
    Task AddRawBytesToBoundedList(string key, byte[] rawBytes);
    Task<byte[]?> GetRawBytesFromBoundedList(string key);
    Task TerminateBoundedList(string key);
  }
}