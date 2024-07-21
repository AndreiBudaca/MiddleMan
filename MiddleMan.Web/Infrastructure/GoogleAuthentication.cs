﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MiddleMan.Core;

namespace MiddleMan.Web.Infrastructure
{
  public static class GoogleAuthentication
  {
    public static void AddGoogleAuthentication(this WebApplicationBuilder builder)
    {
      var services = builder.Services;
      var configuration = builder.Configuration;

      services.AddAuthentication(o =>
      {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
      })
      .AddCookie()
      .AddGoogle(googleOptions =>
      {
        googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        googleOptions.ClientId = configuration[ConfigurationConstants.GoogleAuthentication.ClientId];
        googleOptions.ClientSecret = configuration[ConfigurationConstants.GoogleAuthentication.ClientSecret];
      });
    }
  }
}