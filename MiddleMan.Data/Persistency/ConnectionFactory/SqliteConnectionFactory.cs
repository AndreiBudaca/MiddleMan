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

  public class SqliteDatabaseInitializer
  {
    private static readonly object _lock = new();

    public static void Initialize(IDbConnectionFactory connectionFactory)
    {
      lock (_lock)
      {
        using var connection = connectionFactory.CreateConnection();

        connection.Open();
        var tableCmd = connection.CreateCommand();
        tableCmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS Clients (
            UserId TEXT NOT NULL,
            Name TEXT NOT NULL,
            MethodInfoUrl TEXT,
            LastConnectedAt TEXT NULL,
            Signatures BLOB,
            TokenHash BLOB,
            PRIMARY KEY (UserId, Name)
        );";
        tableCmd.ExecuteNonQuery();
      }
    }
  }
}
