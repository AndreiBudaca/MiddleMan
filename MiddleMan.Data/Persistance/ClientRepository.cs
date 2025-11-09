using Dapper;
using MiddleMan.Data.Persistance.Classes;
using MiddleMan.Data.Persistance.ConnectionFactory;
using MiddleMan.Data.Persistance.Entities;

namespace MiddleMan.Data.Persistance
{
  public interface IClientRepository : IRepository<Client, (string clientId, string name)> { }

  public class ClientRepository(IDbConnectionFactory dbConnectionFactory) : IClientRepository
  {
    private readonly IDbConnectionFactory _connectionFactory = dbConnectionFactory;

    public async Task<(string clientId, string name)> AddAsync(Client entity)
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = @"INSERT INTO Clients (UserId, Name, MethodInfoUrl, LastConnectedAt, IsConnected, Signatures)
                    VALUES (@UserId, @Name, @MethodInfoUrl, @LastConnectedAt, @IsConnected, @Signatures);
                    SELECT @UserId AS clientId, @Name AS name;";
      return await connection.QuerySingleAsync<(string clientId, string name)>(query, entity);
    }

    public async Task DeleteAsync((string clientId, string name) key)
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = "DELETE FROM Clients WHERE UserId = @clientId AND Name = @name;";
      await connection.ExecuteAsync(query, new { key.clientId, key.name });
    }

    public Task<bool> ExistsAsync((string clientId, string name) key)
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = "SELECT COUNT(1) FROM Clients WHERE UserId = @clientId AND Name = @name;";
      return connection.ExecuteScalarAsync<bool>(query, new { key.clientId, key.name });
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = "SELECT * FROM Clients;";
      return await connection.QueryAsync<Client>(query);
    }

    public async Task<Client?> GetByIdAsync((string clientId, string name) key)
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = "SELECT * FROM Clients WHERE UserId = @clientId AND Name = @name;";
      return await connection.QuerySingleOrDefaultAsync<Client>(query, new { key.clientId, key.name });
    }

    public async Task UpdateAsync(Client entity)
    {
      using var connection = _connectionFactory.CreateConnection();
      var query = @"UPDATE Clients
                    SET MethodInfoUrl = @MethodInfoUrl,
                        LastConnectedAt = @LastConnectedAt,
                        IsConnected = @IsConnected,
                        Signatures = @Signatures
                    WHERE UserId = @UserId AND Name = @Name;";
      await connection.ExecuteAsync(query, entity);
    }

    public async Task UpdateAsync((string clientId, string name) key, List<ColumnInfo> updateValues)
    {
      using var connection = _connectionFactory.CreateConnection();

      var setClauses = string.Join(", ", updateValues.Select(p => $"{p.ColumnName} = @{p.ColumnName}"));
      var query = $"UPDATE Clients SET {setClauses} WHERE UserId = @clientId AND Name = @name;";

      var parameters = new DynamicParameters();
      parameters.Add("clientId", key.clientId);
      parameters.Add("name", key.name);

      foreach (var item in updateValues)
      {
        parameters.Add(item.ColumnName, item.Value);
      }

      await connection.ExecuteAsync(query, parameters);
    }

    public async Task<IEnumerable<Client>> GetByContitions(List<ColumnInfo> searchValues)
    {
      using var connection = _connectionFactory.CreateConnection();

      var whereClauses = string.Join("AND", searchValues.Select(p => $"{p.ColumnName} = @{p.ColumnName}"));
      var query = $"SELECT * FROM Clients WHERE {whereClauses};";

      var parameters = new DynamicParameters();

      foreach (var item in searchValues)
      {
        parameters.Add(item.ColumnName, item.Value);
      }

      return await connection.QueryAsync<Client>(query, parameters);
    }
  }
}
