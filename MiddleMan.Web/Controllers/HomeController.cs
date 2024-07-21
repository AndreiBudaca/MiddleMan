﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiddleMan.Web.Controllers
{
  [Route("/")]
  [AllowAnonymous]
  public class HomeController : Controller
  {
    public IActionResult Index()
    {
      return View();
    }
  }
}