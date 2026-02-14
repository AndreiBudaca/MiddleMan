namespace MiddleMan.Data.Persistency.Entities
{
  public class Client
  {
    public required string UserId { get; set; }

    public required string Name { get; set; }

    public string? MethodInfoUrl { get; set; }

    public byte[]? Signatures { get; set; }

    public byte[]? TokenHash { get; set; }

    public class Columns
    {
      public const string UserId = "UserId";
      public const string Name = "Name";
      public const string MethodInfoUrl = "MethodInfoUrl";
      public const string Signatures = "Signatures";
      public const string TokenHash = "TokenHash";
    }
  }
}
