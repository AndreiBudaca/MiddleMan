using System.Collections.Concurrent;

namespace MiddleMan.Core.Extensions
{
  public static class DictionaryExtensions
  {
    /// <summary>
    /// Adds a value to a list in a ConcurrentDictionary. If the key does not exist, it creates a new list with the value. If the key exists, it adds the value to the existing list.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="dictionary">The ConcurrentDictionary to modify.</param>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The updated list.</returns>
    public static List<TValue> AddOrUpdate<TKey, TValue>(this ConcurrentDictionary<TKey, List<TValue>> dictionary, TKey key, TValue value) where TKey : notnull
    {
      return dictionary.AddOrUpdate(key,
        _ => [value],
        (_, list) => { list.Add(value); return list; }
      );
    }
  }
}