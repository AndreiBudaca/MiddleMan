using System.ComponentModel.DataAnnotations;

namespace MiddleMan.Web.Controllers.Clients.Model
{
  public class NewClientModel
  {
    [Required(AllowEmptyStrings = false)]
    [Length(1, 255)]
    public string Name { get; set; } = string.Empty;
  }
}
