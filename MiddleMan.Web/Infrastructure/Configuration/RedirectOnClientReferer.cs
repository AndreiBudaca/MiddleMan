namespace MiddleMan.Web.Infrastructure.Configuration
{
  public class RedirectIndicator
  {
    public required string PathIndicator { get; set; } = string.Empty;

    public int RefererPartsToKeep { get; set; }
  }

  public static class RedirectOnClientReferer
  {
    private static readonly RedirectIndicator[] RefererPathIndicators =
    [
      new()
      {
        PathIndicator = "/client-portal",
        RefererPartsToKeep = 5
      }
    ];

    public static void UseRedirectOnClientReferer(this WebApplication app)
    {
      app.Use(async (context, next) =>
      {
        var referer = context.Request.Headers.Referer.ToString();

        var validReferer = Uri.TryCreate(referer, UriKind.Absolute, out var refererUri);
        if (!validReferer)
        {
          await next.Invoke(context);
          return;
        }

        foreach (var indicator in RefererPathIndicators)
        {
          if (refererUri!.LocalPath.StartsWith(indicator.PathIndicator) && !context.Request.Path.ToString().StartsWith(indicator.PathIndicator))
          {
            var refererPathParts = refererUri!.LocalPath.Split('/');

            var newUrl =
                $"{refererUri.Scheme}://{refererUri.Authority}" +
                $"{string.Join('/', refererPathParts.Take(indicator.RefererPartsToKeep))}{context.Request.Path}{context.Request.QueryString}";

            context.Response.Redirect(newUrl, true, true);
            return;
          }
        }

        await next.Invoke(context);
      });
    }
  }
}
