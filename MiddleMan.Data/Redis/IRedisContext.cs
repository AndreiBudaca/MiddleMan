
namespace MiddleMan.Data.Redis
{
  public interface IRedisContext
  {
    Task AddToList<T>(string listKey, T element);
    Task RemoveFromList<T>(string listKey, T element);

    Task<bool> ExistsInHash(string hashKey, string elementKey);
    Task AddToHash<T>(string hashKey, string elementKey, T element);
    Task<T?> GetFromHash<T>(string hashKey, string elementKey);
    Task<Dictionary<string, T?>> GetAllFromHash<T>(string hashKey);
    Task RemoveFromHash(string hashKey, string elementKey);
  }
}
