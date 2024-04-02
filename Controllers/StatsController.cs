using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.StatisticsModels;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class StatsController : Controller
    {
        private readonly ILogger<StatsController> _logger;
        StatsControllerConfig config = new StatsControllerConfig();
        const string statsControllerConfigPath = @"c:\core\mes\ControllerConfig\StatsController.json";
        const string intranetLog = @"c:\temp\intranet.log";        

        public StatsController(ILogger<StatsController> logger)
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(statsControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<StatsControllerConfig>(rawConf);            
            
            _logger = logger;
        }

        public IActionResult Index()
        {
            List<string> machineNames = config.AvailableMachines
                                                .Select(machine => machine.MachineName)
                                                .ToList();
            ViewBag.machineNames = machineNames;

            GeneralPurpose genPurpose = new GeneralPurpose();
            //Date
            //int interval = config.DaysDisplayedDefault;
            ViewBag.startTime = genPurpose.GetWeeksMonday(0);
            ViewBag.endTime = genPurpose.GetWeeksMonday(5);

            return View();
        }        

        public IActionResult MachineDetail(string machineName, string startTime, string endTime)
        {
            //string MachineName
            MachineDetails machineDetails = config.AvailableMachines.Where(m =>m.MachineName == machineName).FirstOrDefault();
            
            GeneralPurpose genPurpose = new GeneralPurpose();
            string startTime2 = genPurpose.GetWeeksMonday(-machineDetails.DaysDisplayedDefault).ToString();
            string endTime2 = genPurpose.GetWeeksMonday(0).ToString();

            StatisticsService statService = new StatisticsService();
            List<DayStatistic> machineStatistics = statService.GetMachineStats(machineDetails, startTime2, endTime2);

            ViewBag.machineName = machineName;
            ViewBag.defaultDate = DateTime.Now.ToString("yyyy-MM-dd");

            //---------------------- settimana dal al
            //GeneralPurpose genPurpose = new GeneralPurpose();

            ViewBag.startWeek = genPurpose.GetWeeksMonday(0).ToString("dd/MM/yyyy");
            ViewBag.endWeek = genPurpose.GetWeeksMonday(5).ToString("dd/MM/yyyy");           

            if(!machineStatistics[0].IsAlive)
            {
                //ViewBag.errorMsg = "macchina spenta o scollegata";
            }

            //TO DO: creare tre liste
            //
            // minuti di macchina accesa per ogni giorno
            // minuti di lavorazione (quando disponibili) per ogni 
            // giorni della settimana nel periodo
            // text: pannelli stefani
            //

            var data1 = new List<int> {180, 55, 41, 37, 22, 43};
            ViewBag.Data1 = data1;

            return View(machineStatistics);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }


    }
}