namespace MiddleMan.Data.InMemory
{
  public interface IInMemoryContext
  {
    #region [Hash Operations]
    Task<bool> ExistsInHash(string hashKey, string elementKey);
    Task AddToHash<T>(string hashKey, string elementKey, T element);
    Task<T?> GetFromHash<T>(string hashKey, string elementKey);
    Task<Dictionary<string, T?>> GetAllFromHash<T>(string hashKey);
    Task RemoveFromHash(string hashKey, string elementKey);
    #endregion

    #region [List Operations]
    Task AddToList<T>(string listKey, T element);
    Task RemoveFromList<T>(string listKey, T element);
    Task<List<T?>> GetAllFromList<T>(string listKey);
    Task<T?> GetRandomFromList<T>(string listKey);
    Task<long> ListCount(string listKey);
    #endregion
  }
}
