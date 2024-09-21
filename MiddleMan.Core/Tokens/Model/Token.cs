namespace MiddleMan.Core.Tokens.Model
{
  public class Token
  {
    public string Identifier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime ValidTill { get; set; }
  }
}
