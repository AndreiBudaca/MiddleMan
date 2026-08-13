using Microsoft.Data.Sqlite;
using System.Data;

namespace MiddleMan.Data.Persistency.ConnectionFactory
{
  public class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
  {
    private readonly string _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
      return new SqliteConnection(_connectionString);
    }
  }
}
