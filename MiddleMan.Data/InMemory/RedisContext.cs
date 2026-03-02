using MiddleMan.Core;
using NRedisStack;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace MiddleMan.Data.InMemory
{
  public class RedisContext : ISharedInMemoryContext
  {
    private readonly IDatabase database;

    private static string BoundedTokensKey(string key) => $"{key}:tokens";
    private static string BoundedChunksKey(string key) => $"{key}:chunks";

    #region [Hash Operations]
    public RedisContext(string connectionString)
    {
      ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
      {
        EndPoints = { connectionString },
        ConnectTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
      });
      database = redis.GetDatabase();
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
    #endregion

    #region [List Operations]
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

    public async Task<T?> GetRandomFromList<T>(string listKey)
    {
      var lua = @"
        local len = redis.call('LLEN', KEYS[1])
        if len == 0 then
          return nil
        end
        if len == 1 then
          return redis.call('LINDEX', KEYS[1], 0)
        end
        local t = redis.call('TIME')
        math.randomseed(tonumber(t[2]))
        local idx = math.random(0, len-1)
        return redis.call('LINDEX', KEYS[1], idx)
        ";

      var result = await database.ScriptEvaluateAsync(lua, [listKey]);
      if (result.IsNull) return default;

      return JsonSerializer.Deserialize<T>(result.ToString());
    }

    public Task<long> ListCount(string listKey)
    {
      return database.ListLengthAsync(listKey);
    }

    public async Task<List<T?>> GetAllFromList<T>(string listKey)
    {
      var elements = await database.ListRangeAsync(listKey);
      return elements.Select(e => JsonSerializer.Deserialize<T>(e.ToString())).ToList();
    }
    #endregion

    #region [Bounded lists]
    public async Task CreateBoundedList(string key, int maxCount)
    {
      var values = Enumerable.Repeat(RedisValue.EmptyString, maxCount).ToArray();
      await database.ListRightPushAsync(BoundedTokensKey(key), values);
    }

    public async Task AddRawBytesToBoundedList(string key, byte[] rawBytes)
    {
      _ = await database.BLPopAsync(BoundedTokensKey(key), ServerCapabilities.GlobalTimeoutSeconds);
      await database.ListRightPushAsync(BoundedChunksKey(key), rawBytes);
      await database.KeyExpireAsync(BoundedChunksKey(key), TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));
    }

    public async Task<byte[]?> GetRawBytesFromBoundedList(string key)
    {
      var response = await database.ExecuteAsync("BLPOP", BoundedChunksKey(key), ServerCapabilities.GlobalTimeoutSeconds);
      await database.ListRightPushAsync(BoundedTokensKey(key), RedisValue.EmptyString);
      await database.KeyExpireAsync(BoundedTokensKey(key), TimeSpan.FromSeconds(ServerCapabilities.GlobalTimeoutSeconds));

      if (response.IsNull) return null;
      var responseArray = (RedisResult[]?)response;
      if (responseArray == null || responseArray.Length < 2) return null;
      var bytes = (byte[]?)responseArray[1];
      
      return bytes;
    }

    public async Task TerminateBoundedList(string key)
    {
      await database.KeyDeleteAsync(BoundedTokensKey(key));
    }
    #endregion
  }
}
