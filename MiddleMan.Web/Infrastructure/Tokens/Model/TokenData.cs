using MiddleMan.Web.Infrastructure.Tokens.Constants;

namespace MiddleMan.Web.Infrastructure.Tokens.Model
{
  public class TokenData
  {
    public string Identifier { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Validity { get; set; } = TokenConstants.DefaultValidity;

    public string? Secret { get; set; }
  }
}
