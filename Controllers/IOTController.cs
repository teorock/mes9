using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class IOTController : Controller
    {
        private readonly ILogger<IOTController> _logger;

        public IOTController(ILogger<IOTController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ReceiveData(string jsonData)
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}