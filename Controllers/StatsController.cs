using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.StatisticsModels;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceStack.Text;

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
            //qui vengono generate le date di default per la prima visione
            ViewBag.startTime = genPurpose.GetWeeksMonday(0);
            ViewBag.endTime = genPurpose.GetWeeksMonday(5);

            return View();
        }        

        public IActionResult MachineDetail(string machineName, string startTime, string endTime)
        {
            StatisticsService statService = new StatisticsService();
            GeneralPurpose genPurpose = new GeneralPurpose();

            MachineDetails machineDetails = config.AvailableMachines.Where(m =>m.MachineName == machineName).FirstOrDefault();                        
            
            List<DayStatistic> machineStatistics = statService.GetMachineStats(machineDetails, startTime, endTime);
            //dati arrivati
            ViewBag.machineName = machineName;
            ViewBag.defaultDate = DateTime.Now.ToString("yyyy-MM-dd");

            ViewBag.startWeek = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
            ViewBag.endWeek = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd");

            if(!machineStatistics[0].IsAlive)
            {
                ViewBag.errorMsg = "macchina spenta o scollegata";
            }

            List<int> onTime;
            List<int> workingTime;
            List<string> daysNames;
            List<int> progsPerDay;
            List<double> progsPerHour;      
            List<double> totalMeters;    
            List<double> totalMetersConsumed;  

            statService.FormatMachineData(machineStatistics,machineDetails.MachineType, out onTime, out workingTime, out daysNames, out progsPerDay, out progsPerHour, out totalMeters, out totalMetersConsumed);

            ViewBag.OnTime = onTime;
            ViewBag.WorkingTime = workingTime;
            ViewBag.Days = daysNames;
            ViewBag.ProgsXDays = progsPerDay;
            ViewBag.ProgramsPerHour = progsPerHour;
            ViewBag.TotalMeters = totalMeters;
            ViewBag.TotalMetersConsumed = totalMetersConsumed;

            ViewBag.MaxDate = DateTime.Now.ToString("yyyy-MM-dd");

            ViewBag.Comment = $"Dati per {machineName} dal {Convert.ToDateTime(startTime).ToString("dd/MMM")} al {Convert.ToDateTime(endTime).ToString("dd/MMM")}";

            return View(machineStatistics);
        }

        public IActionResult ExportStats2Csv(string machineName, string startTime, string endTime)
        {
            StatisticsService statService = new StatisticsService();
            GeneralPurpose genPurpose = new GeneralPurpose();

            MachineDetails machineDetails = config.AvailableMachines.Where(m =>m.MachineName == machineName).FirstOrDefault();                        
            
            List<DayStatistic> machineStatistics = statService.GetMachineStats(machineDetails, startTime, endTime);

            //string csv = CsvSerializer.SerializeToCsv(exportBordi);
            List<string> csvList = genPurpose.ExportObj2CsvList<DayStatistic>(machineStatistics);
            string csvContent = genPurpose.List2Csv(csvList);

            string outputFile = $"{machineName}_{startTime}_{endTime}.csv";

            byte[] bytes = Encoding.UTF8.GetBytes(csvContent.ToString());

            return File(bytes, "text/csv", outputFile);                   
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}