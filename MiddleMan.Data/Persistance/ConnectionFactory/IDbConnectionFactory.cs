using System.Data;

namespace MiddleMan.Data.Persistance.ConnectionFactory
{
  public interface IDbConnectionFactory
  {
    IDbConnection CreateConnection();
  }
}
