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
    }
  }
}
