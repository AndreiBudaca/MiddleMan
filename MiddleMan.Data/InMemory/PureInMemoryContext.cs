namespace MiddleMan.Data.InMemory
{
  public class PureInMemoryContext : IInMemoryContext
  {
    private readonly Dictionary<string, Dictionary<string, object?>> _hashes = [];
    private readonly Dictionary<string, List<object?>> _lists = [];
    private readonly object _lock = new();

    #region [Hash Operations]
    public void AddToHash<T>(string hashKey, string elementKey, T element)
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
    }

    public bool ExistsInHash(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);
        if (!hashExists)
        {
          return false;
        }

        var elementExists = hash!.ContainsKey(elementKey);
        return elementExists;
      }
    }

    public Dictionary<string, T?> GetAllFromHash<T>(string hashKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);

        return hashExists ? hash!.ToDictionary(kvp => kvp.Key, kvp => (T?)kvp.Value) : [];
      }
    }

    public T? GetFromHash<T>(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash);
        if (!hashExists)
        {
          return default;
        }

        var elementExists = hash!.TryGetValue(elementKey, out object? element);
        return elementExists ? (T?)element : default;
      }
    }

    public void RemoveFromHash(string hashKey, string elementKey)
    {
      lock (_lock)
      {
        var hashExists = _hashes.TryGetValue(hashKey, out Dictionary<string, object?>? hash) && hash != null;
        if (hashExists)
        {
          hash!.Remove(elementKey);
          if (hash.Count == 0)
          {
            _hashes.Remove(hashKey);
          }
        }
      }
    }
    #endregion

    #region [List Operations]
    public int AddToList<T>(string listKey, T element)
    {
      lock (_lock)
      {
        if (!_lists.TryGetValue(listKey, out List<object?>? list))
        {
          list = [];
          _lists[listKey] = list;
        }

        list.Add(element);
        return list.Count;
      }
    }

    public int AddToList<T>(string listKey, IEnumerable<T> elements)
    {
      lock (_lock)
      {
        if (!_lists.TryGetValue(listKey, out List<object?>? list))
        {
          list = [];
          _lists[listKey] = list;
        }

        foreach (var element in elements)
        {
          list.Add(element);
        }

        return list.Count;
      }
    }

    public int RemoveFromList<T>(string listKey, T element)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          list.Remove(element);
          if (list.Count == 0)
          {
            _lists.Remove(listKey);
          }
        }
        return list?.Count ?? 0;
      }
    }

    public void RemoveList(string listKey)
    {
      lock (_lock)
      {
        _lists.Remove(listKey);
      }
    }

    public T? GetRandomFromList<T>(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list) && list.Count > 0)
        {
          if (list.Count == 1)
          {
            return (T?)list[0];
          }

          var random = new Random();
          int index = random.Next(list.Count);
          return (T?)list[index];
        }
        
        return default;
      }
    }

    public int ListCount(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          return list.Count;
        }

        return 0;
      }
    }

    public bool ExistsList(string listKey)
    {
      lock (_lock)
      {
        return _lists.ContainsKey(listKey);
      }
    }

    public List<T?> GetAllFromList<T>(string listKey)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list))
        {
          return list.Cast<T?>().ToList();
        }

        return [];
      }
    }

    public T? PopList<T>(string listKey, bool removeListIfEmpty = true)
    {
      lock (_lock)
      {
        if (_lists.TryGetValue(listKey, out List<object?>? list) && list.Count > 0)
        {
          var element = list[0];
          list.RemoveAt(0);
          if (removeListIfEmpty && list.Count == 0)
          {
            _lists.Remove(listKey);
          }
          return (T?)element;
        }

        return default;
      }
    }
    #endregion
  }
}
