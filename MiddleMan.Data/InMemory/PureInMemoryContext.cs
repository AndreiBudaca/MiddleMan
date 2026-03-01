namespace MiddleMan.Data.InMemory
{
  public class PureInMemoryContext : IInMemoryContext
  {
    private readonly Dictionary<string, Dictionary<string, object?>> _hashes = [];
    private readonly Dictionary<string, List<object?>> _lists = [];
    private readonly object _lock = new();

    #region [Hash Operations]
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
    #endregion

    #region [List Operations]
    public Task AddToList<T>(string listKey, T element)
    {
      lock (_lock)
      {
        if (!_lists.TryGetValue(listKey, out List<object?>? list))
        {
          list = [];
          _lists[listKey] = list;
        }

        list.Add(element);
      }

      return Task.CompletedTask;
    }

    public Task RemoveFromList<T>(string listKey, T element)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          list.Remove(element);
        }
      }

      return Task.CompletedTask;
    }

    public Task<T?> GetRandomFromList<T>(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list) && list.Count > 0)
        {
          if (list.Count == 1)
          {
            return Task.FromResult((T?)list[0]);
          }

          var random = new Random();
          int index = random.Next(list.Count);
          return Task.FromResult((T?)list[index]);
        }
        
        return Task.FromResult<T?>(default);
      }
    }

    public Task<long> ListCount(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          return Task.FromResult((long)list.Count);
        }

        return Task.FromResult(0L);
      }
    }

    public Task<List<T?>> GetAllFromList<T>(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          return Task.FromResult(list.Cast<T?>().ToList());
        }

        return Task.FromResult(new List<T?>());
      }
    }
    #endregion
  }
}
