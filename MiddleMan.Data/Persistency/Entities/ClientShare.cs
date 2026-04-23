namespace MiddleMan.Data.Persistency.Entities
{
  public class ClientShare
  {
    public required string UserId { get; set; }

    public required string Name { get; set; }

    public required string SharedWithUserEmail { get; set; }

    public class Columns
    {
      public const string UserId = "UserId";
      public const string Name = "Name";
      public const string SharedWithUserEmail = "SharedWithUserEmail";
    }
  }
}