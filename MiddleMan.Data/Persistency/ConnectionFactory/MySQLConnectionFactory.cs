using System.Data;
using MySql.Data.MySqlClient;

namespace MiddleMan.Data.Persistency.ConnectionFactory
{
  public class MySQLConnectionFactory(string connectionString) : IDbConnectionFactory
  {
    private readonly string _connectionString = connectionString;

    public IDbConnection CreateConnection()
    {
      return new MySqlConnection(_connectionString);
    }
  }
}