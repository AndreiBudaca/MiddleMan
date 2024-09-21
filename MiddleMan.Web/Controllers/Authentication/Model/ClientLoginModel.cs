using System.ComponentModel.DataAnnotations;

namespace MiddleMan.Web.Controllers.Authentication.Model
{
  public class ClientLoginModel
  {
    [Required(AllowEmptyStrings = false)]
    [Length(1, 255)]
    public string ClientName { get; set; } = string.Empty;
  }
}
