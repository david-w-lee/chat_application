using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using chat_application.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using chat_application.Database;
using Microsoft.AspNetCore.Http;

namespace chat_application.Controllers
{
    public class AuthController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuthController> _logger;
        private readonly UserService _userService;

        public AuthController(ILogger<AuthController> logger, IMemoryCache cache, UserService userService)
        {
            _logger = logger;
            _cache = cache;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            User dbUser = _userService.Find(user.Username).FirstOrDefault();
            if (dbUser != null)
            {
                var claims = new List<Claim>
                {
                    new Claim("Username", user.Username),
                    new Claim(ClaimTypes.Role, "Administrator"),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties();

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                string cookieValue = GetCookieValueFromResponse(HttpContext.Response, ".AspNetCore.Cookies");
                Startup.SessionIdUserIdDictionary[cookieValue] = dbUser.Id;
                
                return RedirectToAction("index", "home");
            }
            else
            {
                return View();
            }
        }

        private string GetCookieValueFromResponse(HttpResponse response, string cookieName)
        {
            foreach (var headers in response.Headers.Values)
                foreach (var header in headers)
                    if (header.StartsWith($"{cookieName}="))
                    {
                        var p1 = header.IndexOf('=');
                        var p2 = header.IndexOf(';');
                        return header.Substring(p1 + 1, p2 - p1 - 1);
                    }
            return null;
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
              CookieAuthenticationDefaults.AuthenticationScheme);

            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            _userService.Create(user);

            return RedirectToAction("login", "auth");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
