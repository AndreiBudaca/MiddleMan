using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Configuration;
using MiddleMan.Web.Infrastructure.Tokens;
using System.Net;

namespace MiddleMan.Web
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container.
      builder.Services.AddControllersWithViews(options =>
      {
        options.Filters.Add(typeof(ModelValidatorAttribute));
      });

      builder.Services.AddSignalR();

      builder.Services.AddServices();

      builder.Services
        .AddAuthentication(o =>
        {
          o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        })
        .AddGoogleAuthentication(builder.Configuration)
        .AddJWTAuthentication(builder.Configuration);

      builder.Services
        .AddAuthorization(options =>
        {
          options.DefaultPolicy = new AuthorizationPolicyBuilder(
              JwtBearerDefaults.AuthenticationScheme,
              CookieAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
        });

      var app = builder.Build();

      // Configure the HTTP request pipeline.
      if (!app.Environment.IsDevelopment())
      {
        app.UseExceptionHandler("/Error");
      }

      app.UseProxy();

      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();

      app.UseAuthorization();

      app.MapControllers();

      app.MapHubs();

      app.Run();
    }
  }
}
