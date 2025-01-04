using Microsoft.Extensions.Configuration;
using MiddleMan.Core;
using StackExchange.Redis;
using System.Text.Json;

namespace MiddleMan.Data.Redis
{
  public class RedisContext : IRedisContext
  {
    private readonly IDatabase database;

    public RedisContext(IConfiguration configuration)
    {
      ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(configuration.GetConnectionString(ConfigurationConstants.ConnectionStrings.Redis)!);
      database = redis.GetDatabase();
    }

    public async Task AddToList<T>(string listKey, T element)
    {
      var jsonData = JsonSerializer.Serialize(element);
      await database.ListRightPushAsync(listKey, jsonData);
    }

    public async Task RemoveFromList<T>(string listKey, T element)
    {
      var jsonData = JsonSerializer.Serialize(element);
      await database.ListRemoveAsync(listKey, jsonData);
    }

    public async Task<bool> ExistsInHash(string hashKey, string elementKey)
    {
      return await database.HashExistsAsync(hashKey, elementKey);
    }

    public async Task AddToHash<T>(string hashKey, string elementKey, T element)
    {
      var jsonData = JsonSerializer.Serialize(element);
      await database.HashSetAsync(new RedisKey(hashKey), [new HashEntry(elementKey, jsonData)]);
    }

    public async Task RemoveFromHash(string hashKey, string elementKey)
    {
      await database.HashDeleteAsync(hashKey, elementKey);
    }

    public async Task<Dictionary<string, T?>> GetAllFromHash<T>(string hashKey)
    {
      var elements = await database.HashGetAllAsync(hashKey);

      return elements.ToDictionary(entry => entry.Name.ToString(), entry => JsonSerializer.Deserialize<T>(entry.Value!));
    }

    public async Task<T?> GetFromHash<T>(string hashKey, string elementKey)
    {
      var element = await database.HashGetAsync(hashKey, elementKey);
      if (element == RedisValue.Null) return default;

      return JsonSerializer.Deserialize<T>(element!);
    }
  }
}
