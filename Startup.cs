using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using chat_application.Database;
using chat_application.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace chat_application
{
    public class Startup
    {
        public static ConcurrentDictionary<string, string> SessionIdUserIdDictionary = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, WebSocket> UserIdWebSocketDictionary = new ConcurrentDictionary<string, WebSocket>();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Use AddRazorPages() to support WebAPI, MVC and Razor pages in the same project.
            // Use AddRazorRuntimeCompilation() to change views and see changes without restarting project.
            //  Need to install Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.
            services.AddRazorPages().AddRazorRuntimeCompilation();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.LoginPath = new PathString("/auth/login");
                    options.AccessDeniedPath = new PathString("/auth/denied");
                });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
                options.HttpOnly = HttpOnlyPolicy.None;
                options.Secure =  CookieSecurePolicy.Always;
            });

            services.Configure<DatabaseSettings>(Configuration.GetSection(nameof(DatabaseSettings)));

            services.AddSingleton<IDatabaseSettings>(sp => sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);

            services.AddSingleton<UserService>();

            services.AddSingleton<RoomService>();

            services.AddMemoryCache();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120)
            };
            app.UseWebSockets(webSocketOptions);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                        {
                            await HandleClient(context, webSocket);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }
            });


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // The MapRazorPages call ensures that endpoint routing is set up for Razor Pages.
                endpoints.MapRazorPages();

                //// If you wanted to add routing for controllers, you would include endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task HandleClient(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            if (!context.Request.Cookies.ContainsKey(".AspNetCore.Cookies"))
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.Empty, "Unauthorized", CancellationToken.None);
            }
            else
            {
                string sessionId = context.Request.Cookies[".AspNetCore.Cookies"];
                string userId = SessionIdUserIdDictionary[sessionId];
                UserIdWebSocketDictionary[userId] = webSocket;

                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                while (!result.CloseStatus.HasValue)
                {
                    //await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    string[] strs = msg.Split('|');

                    var userService = context.RequestServices.GetService<UserService>();
                    User user = userService.Get(userId);
                    StringBuilder sb = new StringBuilder();
                    string message = sb.Append(user.Username).Append(": ").Append(strs[1]).ToString();
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                    var roomService = context.RequestServices.GetService<RoomService>();
                    Room room = roomService.Get(strs[0]);
                    foreach (var uid in room.UserIds)
                    {
                        WebSocket socket = UserIdWebSocketDictionary[uid];
                        await socket.SendAsync(new ArraySegment<byte>(messageBytes, 0, messageBytes.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    }

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }


        //private async Task Echo(HttpContext context, WebSocket webSocket)
        //{
        //    var buffer = new byte[1024 * 4];
        //    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    while (!result.CloseStatus.HasValue)
        //    {
        //        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
        //        var userId = context.User.Identities.FirstOrDefault();
        //        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //    }
        //    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //}
    }
}
