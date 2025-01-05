using MiddleMan.Service.WebSocketClients.Dto;

namespace MiddleMan.Web.Controllers.WebSockets.Model
{
  public class WebSocketClientMethodArgumentModel
  {
    public string? Name { get; set; }

    public bool IsPrimitive { get; set; }

    public bool IsArray { get; set; }

    public bool IsNullable { get; set; }

    public string? Type { get; set; }

    public List<WebSocketClientMethodArgumentModel> Components { get; set; } = [];

    public WebSocketClientMethodArgumentModel() { }

    public WebSocketClientMethodArgumentModel(WebSocketClientMethodArgumentDto dto)
    {
      Name = dto.Name;
      IsPrimitive = dto.IsPrimitive;
      IsArray = dto.IsArray;
      IsNullable = dto.IsNullable;
      Type = dto.Type;
      Components = dto.Components.Select(x => new WebSocketClientMethodArgumentModel(x)).ToList();
    }
  }
}
