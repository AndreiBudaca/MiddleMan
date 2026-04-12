namespace MiddleMan.Core
{
  public static class ConfigurationConstants
  {
    public static class Authentication
    {
      public static class GoogleAuthentication
      {
        public const string ClientId = "Authentication:Google:ClientId";
        public const string ClientSecret = "Authentication:Google:ClientSecret";
      }

      public static class ClientToken
      {
        public const string Secret = "Authentication:ClientToken:Secret";
      }
    }

    public static class ConnectionStrings
    {
      public const string Redis = "Redis";
      public const string Sqlite = "Sqlite";
      public const string Postgres = "Postgres";
      public const string MySql = "MySql";
    }

    public static class OracleCloud
    {
      public const string User = "OracleCloud:User";
      public const string Fingerprint = "OracleCloud:Fingerprint";
      public const string Tenancy = "OracleCloud:Tenancy";
      public const string Region = "OracleCloud:Region";
      public const string KeyFile = "OracleCloud:KeyFile";
      public const string Namespace = "OracleCloud:Namespace";
      public const string Bucket = "OracleCloud:Bucket";
    }
  }
}
