using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using chat_application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using chat_application.Database;

namespace chat_application.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {

        private readonly IMemoryCache _cache;
        private readonly ILogger<RoomController> _logger;
        private readonly RoomService _roomService;

        public RoomController(ILogger<RoomController> logger, IMemoryCache cache, RoomService roomService)
        {
            _logger = logger;
            _cache = cache;
            _roomService = roomService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult List()
        {
            //Room room = new Room();
            //room.Name = "Second Room";
            //room.UserIds.Add("5fe7b8171ca8de47c070332a");
            //_roomService.Create(room);
            var rooms = _roomService.GetAllRooms();
            return View(rooms);
        }

        public IActionResult Show(string id)
        {
            var room = _roomService.Get(id);
            var userId = Startup.SessionIdUserIdDictionary[HttpContext.Request.Cookies[".AspNetCore.Cookies"]];
            if (!room.UserIds.Contains(userId))
            {
                room.UserIds.Add(userId);
                _roomService.Update(room);
            }
            return View(room);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
