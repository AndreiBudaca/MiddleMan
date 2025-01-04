namespace MiddleMan.Core
{
  public static class ConfigurationConstants
  {
    public static class Authentication
    {
      public static class GoogleAuthentication
      {
        public readonly static string ClientId = "Authentication:Google:ClientId";
        public readonly static string ClientSecret = "Authentication:Google:ClientSecret";
      }

      public static class ClientToken
      {
        public readonly static string Secret = "Authentication:ClientToken:Secret";
      }
    }
    
    public static class ConnectionStrings
    {
      public readonly static string Redis = "Redis";
    }
  }
}
