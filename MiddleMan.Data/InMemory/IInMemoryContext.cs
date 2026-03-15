namespace MiddleMan.Data.InMemory
{
  public interface IInMemoryContext
  {
    #region [Hash Operations]
    bool ExistsInHash(string hashKey, string elementKey);
    void AddToHash<T>(string hashKey, string elementKey, T element);
    T? GetFromHash<T>(string hashKey, string elementKey);
    Dictionary<string, T?> GetAllFromHash<T>(string hashKey);
    void RemoveFromHash(string hashKey, string elementKey);
    #endregion

    #region [List Operations]
    int AddToList<T>(string listKey, T element);
    int AddToList<T>(string listKey, IEnumerable<T> elements);
    int RemoveFromList<T>(string listKey, T element);
    void RemoveList(string listKey);
    List<T?> GetAllFromList<T>(string listKey);
    T? GetRandomFromList<T>(string listKey);
    T? PopList<T>(string listKey, bool removeListIfEmpty = true);
    int ListCount(string listKey);
    bool ExistsList(string listKey);
    #endregion
  }
}
