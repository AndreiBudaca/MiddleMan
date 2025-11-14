namespace MiddleMan.Data.Persistency.Classes
{
  public class ColumnInfo
  {
    public required string ColumnName { get; set; }

    public required object? Value { get; set; }
  }
}
