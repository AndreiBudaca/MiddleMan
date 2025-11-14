using MiddleMan.Data.Persistency.Classes;

namespace MiddleMan.Data.Persistency
{
  public interface IRepository<T, K>
  {
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetByConditions(List<ColumnInfo> searchValues);
    Task<T?> GetByIdAsync(K key);
    Task<bool> ExistsAsync(K key);
    Task<K> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task UpdateAsync(K key, List<ColumnInfo> updateValues);
    Task DeleteAsync(K key);
  }
}
