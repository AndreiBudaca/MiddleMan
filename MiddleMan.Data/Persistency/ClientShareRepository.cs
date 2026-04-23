using System.Data;
using Dapper;
using MiddleMan.Data.Persistency.Classes;
using MiddleMan.Data.Persistency.ConnectionFactory;
using MiddleMan.Data.Persistency.Entities;

namespace MiddleMan.Data.Persistency
{
  public interface IClientShareRepository : IRepository<ClientShare, (string userId, string name, string sharedWithUserEmail)>, IDisposable { }

  public class ClientShareRepository(IDbConnectionFactory dbConnectionFactory) : IClientShareRepository
  {
    private readonly IDbConnection connection = dbConnectionFactory.CreateConnection();

    public async Task<(string userId, string name, string sharedWithUserEmail)> AddAsync(ClientShare entity)
    {
      var query = $@"INSERT INTO ClientShares ({ClientShare.Columns.UserId}, {ClientShare.Columns.Name}, {ClientShare.Columns.SharedWithUserEmail})
                    VALUES (@{ClientShare.Columns.UserId}, @{ClientShare.Columns.Name}, @{ClientShare.Columns.SharedWithUserEmail});
                    SELECT @{ClientShare.Columns.UserId} AS userId, @{ClientShare.Columns.Name} AS name, @{ClientShare.Columns.SharedWithUserEmail} AS sharedWithUserEmail;";

      return await connection.QuerySingleAsync<(string userId, string name, string sharedWithUserEmail)>(query, entity);
    }

    public async Task DeleteAsync((string userId, string name, string sharedWithUserEmail) key)
    {
      var query = $@"DELETE FROM ClientShares WHERE {ClientShare.Columns.UserId} = @userId AND {ClientShare.Columns.Name} = @name AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail";
      await connection.ExecuteAsync(query, new { key.userId, key.name, key.sharedWithUserEmail });
    }

    public async Task<bool> ExistsAsync((string userId, string name, string sharedWithUserEmail) key)
    {
      var query = $@"SELECT COUNT(*) FROM ClientShares WHERE {ClientShare.Columns.UserId} = @userId AND {ClientShare.Columns.Name} = @name AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail";
      var count = await connection.QuerySingleAsync<int>(query, new { key.userId, key.name, key.sharedWithUserEmail });
      return count > 0;
    }

    public async Task<IEnumerable<ClientShare>> GetAllAsync()
    {
      var query = $@"SELECT * FROM ClientShares";
      return await connection.QueryAsync<ClientShare>(query);
    }

    public async Task<IEnumerable<ClientShare>> GetByConditions(List<ColumnInfo> searchValues)
    {
      var whereClauses = string.Join(" AND ", searchValues.Select(p => $"{p.ColumnName} = @{p.ColumnName}"));
      var query = $@"SELECT * FROM ClientShares WHERE {whereClauses}";
      var parameters = new DynamicParameters();
      foreach (var value in searchValues)
      {
        parameters.Add($"@{value.ColumnName}", value.Value);
      }
      return await connection.QueryAsync<ClientShare>(query, parameters);
    }

    public async Task<IEnumerable<ClientShare>> GetByIds(IEnumerable<(string userId, string name, string sharedWithUserEmail)> keys)
    {
      if (!keys.Any()) return [];

      var whereClauses = string.Join(" OR ", keys.Select((k, i) => $"({ClientShare.Columns.UserId} = @userId{i} AND {ClientShare.Columns.Name} = @name{i} AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail{i})"));
      var query = $@"SELECT * FROM ClientShares WHERE {whereClauses}";

      var parameters = new DynamicParameters();

      int index = 0;
      foreach (var (userId, name, sharedWithUserEmail) in keys)
      {
        parameters.Add($"userId{index}", userId);
        parameters.Add($"name{index}", name);
        parameters.Add($"sharedWithUserEmail{index}", sharedWithUserEmail);
        index++;
      }

      return await connection.QueryAsync<ClientShare>(query, parameters);
    }

    public async Task<ClientShare?> GetByIdAsync((string userId, string name, string sharedWithUserEmail) key)
    {
      var query = $@"SELECT * FROM ClientShares WHERE {ClientShare.Columns.UserId} = @userId AND {ClientShare.Columns.Name} = @name AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail";
      return await connection.QueryFirstOrDefaultAsync<ClientShare>(query, new { key.userId, key.name, key.sharedWithUserEmail });
    }

    public async Task UpdateAsync(ClientShare entity)
    {
      var query = $@"UPDATE ClientShares
                    SET {ClientShare.Columns.UserId} = @{ClientShare.Columns.UserId},
                        {ClientShare.Columns.Name} = @{ClientShare.Columns.Name},
                        {ClientShare.Columns.SharedWithUserEmail} = @{ClientShare.Columns.SharedWithUserEmail}
                    WHERE {ClientShare.Columns.UserId} = @userId AND {ClientShare.Columns.Name} = @name AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail;";

      await connection.ExecuteAsync(query, entity);
    }

    public async Task UpdateAsync((string userId, string name, string sharedWithUserEmail) key, List<ColumnInfo> updateValues)
    {
      var setClauses = string.Join(", ", updateValues.Select(p => $"{p.ColumnName} = @{p.ColumnName}"));
      var query = $@"UPDATE ClientShares
                    SET {setClauses}
                    WHERE {ClientShare.Columns.UserId} = @userId AND {ClientShare.Columns.Name} = @name AND {ClientShare.Columns.SharedWithUserEmail} = @sharedWithUserEmail;";

      var parameters = new DynamicParameters();
      foreach (var value in updateValues)
      {
        parameters.Add($"@{value.ColumnName}", value.Value);
      }
      parameters.Add("@userId", key.userId);
      parameters.Add("@name", key.name);
      parameters.Add("@sharedWithUserEmail", key.sharedWithUserEmail);

      await connection.ExecuteAsync(query, parameters);
    }

    public void Dispose()
    {
      connection.Dispose();

      GC.SuppressFinalize(this);
    }
  }
}