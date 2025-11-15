using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using MiddleMan.Core;
using MiddleMan.Web.Infrastructure.Attributes;
using MiddleMan.Web.Infrastructure.Configuration;
using MiddleMan.Web.Infrastructure.Converters;
using MiddleMan.Web.Infrastructure.Tokens;

namespace MiddleMan.Web
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      if (builder.Environment.IsDevelopment())
      {
        builder.Services.AddCors(options =>
        {
          options.AddDefaultPolicy(policy =>
          {
            policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
          });
        });
      }

      builder.Services
        .AddControllers(options =>
        {
          options.Filters.Add(typeof(ModelValidatorAttribute));
        })
        .AddJsonOptions(options =>
        {
          options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        }); ;

      builder.Services.AddSignalR(options =>
      {
        options.StreamBufferCapacity = 1;
      }).AddMessagePackProtocol();

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

      app.MapStaticFiles(ServerCapabilities.UIStaticFilesPath, ServerCapabilities.StaticFilesPath);

      app.UseRouting();

      if (app.Environment.IsDevelopment())
      {
        app.UseCors();
      }

      app.UseAuthentication();

      app.UseAuthorization();

      app.MapControllers();

      app.MapHubs();

      app.InitializeDb();

      app.Run();
    }
  }
}
