namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class ProxyConfiguration
  {
    public static IApplicationBuilder UseProxy(this IApplicationBuilder app)
    {
      var options = new ForwardedHeadersOptions
      {
        ForwardedHeaders =
          Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
          Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
      };
      options.KnownNetworks.Clear();
      options.KnownProxies.Clear();

      return app.UseForwardedHeaders(options);
    }
  }
}
