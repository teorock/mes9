using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.SmartEquipModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class SmartEquip : Controller
    {
        const string smartEquipControllerConfigPath = @"c:\core\mes\ControllerConfig\SmartEquipController.json";
        const string intranetLog = @"c:\temp\intranet.log";
        SmartEquipControllerConfig config = new SmartEquipControllerConfig();
        private readonly ILogger<SmartEquip> _logger;

        public SmartEquip(ILogger<SmartEquip> logger)
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(smartEquipControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<SmartEquipControllerConfig>(rawConf);    

            _logger = logger;
        }

        public IActionResult Index()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<string> machinesNames = dbAccessor.Queryer<SmartEquipmentData>(config.ConnString, config.DbDataTable).Select(m => m.EquipmentName).Distinct().ToList();

            ViewBag.machinesNames = machinesNames;
            return View();
        }

        public IActionResult EquipmentLastInfo(string equipmentName)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            string filter = $"EquipmentName = \'{equipmentName}\'";
            
            SmartEquipmentData lastData = dbAccessor.QueryerFilter<SmartEquipmentData>(config.ConnString, config.DbDataTable, filter).LastOrDefault();

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}