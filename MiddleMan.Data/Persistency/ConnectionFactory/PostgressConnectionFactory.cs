using Npgsql;
using System.Data;

namespace MiddleMan.Data.Persistency.ConnectionFactory
{
  public class PostgresConnectionFactory(string connectionString) : IDbConnectionFactory
  {
    private readonly string _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
      return new NpgsqlConnection(_connectionString);
    }
  }
}
