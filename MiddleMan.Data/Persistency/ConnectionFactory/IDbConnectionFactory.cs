using System.Data;

namespace MiddleMan.Data.Persistency.ConnectionFactory
{
  public interface IDbConnectionFactory
  {
    IDbConnection CreateConnection();
  }
}
