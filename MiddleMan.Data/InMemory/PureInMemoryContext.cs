namespace MiddleMan.Data.InMemory
{
  public class PureInMemoryContext : IInMemoryContext
  {
    private readonly Dictionary<string, Dictionary<string, object?>> _hashes = new();
    private readonly object _lock = new();

    public Task AddToHash<T>(string hashKey, string elementKey, T element)
    {
      lock (_lock)
      {
        if (!_hashes.TryGetValue(hashKey, out Dictionary<string, object?>? value))
        {
          value = [];
          _hashes[hashKey] = value;
        }

        if (!value.ContainsKey(elementKey))
        {
          value.Add(elementKey, element);
        }
        else
        {
          value[elementKey] = element;
        }
      }

      return Task.CompletedTask;
    }

    public Task<bool> ExistsInHash(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);
        if (!hashExists)
        {
          return Task.FromResult(false);
        }

        var elementExists = hash!.ContainsKey(elementKey);
        return Task.FromResult(elementExists);
      }
    }

    public Task<Dictionary<string, T?>> GetAllFromHash<T>(string hashKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);

        return hashExists ? Task.FromResult(hash!.ToDictionary(kvp => kvp.Key, kvp => (T?)kvp.Value))
          : Task.FromResult(new Dictionary<string, T?>());
      }
    }

    public Task<T?> GetFromHash<T>(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);
        if (!hashExists)
        {
          return Task.FromResult<T?>(default);
        }

        var elementExists = hash!.TryGetValue(elementKey, out object? element);
        return Task.FromResult(elementExists ? (T?)element : default);
      }
    }

    public Task RemoveFromHash(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);
        if (hashExists)
        {
          hash!.Remove(elementKey);
        }

        return Task.CompletedTask;
      }
    }
  }
}
