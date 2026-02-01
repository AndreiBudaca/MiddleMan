using MiddleMan.Web.Communication.Metadata.Constants;

namespace MiddleMan.Web.Communication.Metadata
{
  public class HttpUser
  {
    public string Identifier { get; set; } = string.Empty;

    public string Role { get; set; } = UserTypes.User;
  }
}
