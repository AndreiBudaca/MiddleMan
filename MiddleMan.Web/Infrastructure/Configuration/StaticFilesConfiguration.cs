using Microsoft.Extensions.FileProviders;

namespace MiddleMan.Web.Infrastructure.Configuration
{
  public static class StaticFilesConfiguration
  {
    public static void MapStaticFiles(this WebApplication app, params string[] paths)
    {
      var compositeProvider = new CompositeFileProvider(paths.Select(p => new PhysicalFileProvider(p)));

      app.UseDefaultFiles(new DefaultFilesOptions
      {
        FileProvider = compositeProvider,
      });

      app.UseStaticFiles(new StaticFileOptions
      {
        FileProvider = compositeProvider,
      });
    }
  }
}
