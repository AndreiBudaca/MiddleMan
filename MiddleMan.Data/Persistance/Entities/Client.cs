namespace MiddleMan.Data.Persistance.Entities
{
  public class Client
  {
    public required string UserId { get; set; }

    public required string Name { get; set; }

    public string? MethodInfoUrl { get; set; }

    public required DateTime LastConnectedAt { get; set; }

    public required bool IsConnected { get; set; }

    public byte[]? Signatures { get; set; }

    public class Columns
    {
      public const string UserId = "UserId";
      public const string Name = "Name";
      public const string MethodInfoUrl = "MethodInfoUrl";
      public const string LastConnectedAt = "LastConnectedAt";
      public const string IsConnected = "IsConnected";
      public const string Signatures = "Signatures";
    }
  }
}
