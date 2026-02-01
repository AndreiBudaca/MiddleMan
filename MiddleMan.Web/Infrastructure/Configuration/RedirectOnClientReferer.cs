namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class RedirectOnClientReferer
  {
    public static void UseRedirectOnClientReferer(this WebApplication app)
    {
      app.Use(async (context, next) =>
      {
        var referer = context.Request.Headers.Referer.ToString();

        if (!string.IsNullOrEmpty(referer)
            && referer.Contains("api/websockets")
            && !context.Request.Path.ToString().Contains("api/websockets")
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
          var refererPathParts = refererUri.LocalPath.Split('/');

          var newUrl =
              $"{refererUri.Scheme}://{refererUri.Authority}" +
              $"{string.Join('/', refererPathParts.Take(5))}{context.Request.Path}{context.Request.QueryString}";

          context.Response.Redirect(newUrl, true, true);
          return;
        }

        await next.Invoke(context);
      });
    }
  }
}
