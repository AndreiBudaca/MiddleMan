using System.Collections.Concurrent;

namespace MiddleMan.Core.Extensions
{
  public static class DictionaryExtensions
  {
    public static List<TValue> AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) where TKey : notnull
    {
      return dictionary.AddOrUpdate(key,
        _ => [value],
        (_, list) => { list.Add(value); return list; }
      );
    }
  }
}