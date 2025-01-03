using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MiddleMan.Web.Hubs
{
  [Authorize]
  public class PlaygroundHub : Hub
  {
    public async Task DoSmth(string message)
    {
      var x = Context.User;

      await Clients.All.SendAsync("ReceiveMessage", message);
    }
  }
}
