using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.StatisticsModels;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Roles ="root, StatisticViewer")]
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

        [Authorize(Roles ="root, StatisticViewer")]
        public IActionResult MachineDetail(string machineName, string startTime, string endTime)
        {
            int daysGap = (Convert.ToDateTime(endTime) - Convert.ToDateTime(startTime)).Days;

            StatisticsService statService = new StatisticsService();
            GeneralPurpose genPurpose = new GeneralPurpose();

            MachineDetails machineDetails = config.AvailableMachines.Where(m =>m.MachineName == machineName).FirstOrDefault();
            //semaforo qui-------------------- qui decide come e dove andare a prendere i dati
            List<DayStatistic> machineStatistics = statService.GetMachineStats(machineDetails, startTime, endTime);                                                                                                    

            if(machineStatistics.Count()==0)
            {
                ViewBag.errorMsg = "Nessun dato per il periodo selezionato oppure macchina spenta o scollegata";
                ViewBag.startWeek = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
                ViewBag.endWeek = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd");  
                ViewBag.machineName = machineName;              
                return View(machineStatistics);
            }            

            if(daysGap != 0)
            {
                ViewBag.machineName = machineName;
                ViewBag.defaultDate = DateTime.Now.ToString("yyyy-MM-dd");

                ViewBag.startWeek = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
                ViewBag.endWeek = Convert.ToDateTime(endTime).ToString("yyyy-MM-dd");

                List<int> onTime;
                List<int> workingTime;
                List<string> daysNames;
                List<int> progsPerDay;
                List<double> progsPerHour;      
                List<double> totalMeters;    
                List<double> totalMetersConsumed;  

                statService.FormatMachineData(machineStatistics, machineDetails.MachineType, out onTime, out workingTime, out daysNames, out progsPerDay, out progsPerHour, out totalMeters, out totalMetersConsumed);

                ViewBag.OnTime = onTime;
                ViewBag.WorkingTime = workingTime;
                ViewBag.Days = daysNames;
                ViewBag.ProgsXDays = progsPerDay;
                ViewBag.ProgramsPerHour = progsPerHour;
                ViewBag.TotalMeters = totalMeters;
                ViewBag.TotalMetersConsumed = totalMetersConsumed;

                ViewBag.MaxDate = DateTime.Now.ToString("yyyy-MM-dd");

                ViewBag.Comment = $"Dati per {machineName} dal {Convert.ToDateTime(startTime).ToString("dd/MMM")} al {Convert.ToDateTime(endTime).ToString("dd/MMM")}";
                ViewBag.machineType = machineDetails.MachineType;
                
                switch(machineDetails.MachineType)
                {
                    case "SCM2":
                        ViewBag.entityName = "lati";
                        break;

                    case "BIESSE1":
                        ViewBag.entityName = "pezzi";
                        break;

                    default:
                        ViewBag.entityName = "pezzi";
                        break;
                }

                return View(machineStatistics);
            }
            else //daysGap = 0
            {
                //bisogna gestire le differenti maniere di estrarre la statistica oraria per tipo macchina (file differenti)
                List<HourStatistics> hourStats = new List<HourStatistics>();
                
                switch (machineDetails.MachineType)
                {
                    case "SCM2":
                        hourStats = statService.FormatSCM2MachineDailyData(machineDetails, startTime, endTime, out List<string> xLabels, out string title);
                        //area test e debug
                        string series = statService.SeriesDataStringBuilder(hourStats);
                        ViewBag.Series = series;

                        string categories = "[" + string.Join(", ", xLabels.Select(x => "\"" + x + "\"")) + "]";
                        ViewBag.Categories = categories;

                        // stringa di titolo con totale giornaliero
                        ViewBag.title = title;
                        // data di default per widget calendario
                        ViewBag.defaultDate = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
                        ViewBag.machineName = $"{machineName}";
                        ViewBag.MaxDate = DateTime.Now.ToString("yyyy-MM-dd");  
                        ViewBag.machineType = machineDetails.MachineType;
                        ViewBag.entityName ="lati";                  
                        break;

                    case "BIESSE1":
                        hourStats = statService.FormatBIESSE1MachineDailyData(machineDetails, startTime, endTime, out List<string> biesseXLabels, out string biessetitle);

                        string bsSeries = statService.SeriesDataStringBuilder(hourStats);
                        ViewBag.Series = bsSeries;

                        string biesseCategories = "[" + string.Join(", ", biesseXLabels.Select(x => "\"" + x + "\"")) + "]";
                        ViewBag.Categories = biesseCategories;

                        // stringa di titolo con totale giornaliero
                        ViewBag.title = biessetitle;
                        // data di default per widget calendario
                        ViewBag.defaultDate = Convert.ToDateTime(startTime).ToString("yyyy-MM-dd");
                        ViewBag.machineName = $"{machineName}";
                        ViewBag.MaxDate = DateTime.Now.ToString("yyyy-MM-dd");
                        ViewBag.machineType = machineDetails.MachineType;
                        ViewBag.entityName ="pezzi";                        
                        break;
                }
                
                return View("MachineDailyDetail", machineStatistics);
            }

            //return View(machineStatistics);
        }

        [Authorize(Roles ="root, StatisticViewer")]
        public IActionResult ExportStats2Csv(string machineName, string startTime, string endTime)
        {
            //per poter suddividere i risultati per bordi, è necessario riestrarre i dati grezzi dalla macchina e dividerli per giorno/spessore
            StatisticsService statService = new StatisticsService();
            GeneralPurpose genPurpose = new GeneralPurpose();

            MachineDetails machineDetails = config.AvailableMachines.Where(m =>m.MachineName == machineName).FirstOrDefault();                        
            List<string>csvList = new List<string>();

            // da gestire i dati per tipo macchina
            // se SCM2 e isAlive = false, esci con errore
            // se BIESSE1 -- estrai per biesse 1

            switch (machineDetails.MachineType)
            {
                case "BIESSE1":
                    //
                    List<DayStatistic> dayStats = statService.GetBIESSE1Data(machineDetails, startTime, endTime, machineDetails.FtpTempFolder);
                    List<BIESSE1ReportBody> transformed = statService.BIESSE1ReportMapper(dayStats);
                    csvList = genPurpose.ExportObj2CsvList<BIESSE1ReportBody>(transformed); 
                    break;

                case "SCM2":
                    List<SCM2ReportBody> rawReportBodies = statService.GetSCM2WebRawData(machineDetails, startTime, endTime);

                    //lista dei giorni
                    List<DateTime> days = new List<DateTime>();
                    List<SCM2ReportToFile> report = new List<SCM2ReportToFile>();

                    if(rawReportBodies != null)
                    {
                        days = rawReportBodies.Select(d => Convert.ToDateTime(d.DateTime).Date).Distinct().ToList();
                                                    
                        //per ogni giorno creo una lista
                        foreach(DateTime oneDay in days)
                        {
                            List<SCM2ReportBody> oneDayList = rawReportBodies.Where(d => d.DateTime.Date == oneDay.Date).ToList();                
                            //estraggo il numero d bordi in questa lista
                            List<KeyValuePair<int, double>> thicknessPieces = statService.GetThicknessPieces(rawReportBodies, oneDay);
                            //estraggo la lista raw solo per quello spessore
                            foreach(var oneCouple in thicknessPieces)
                            {
                                List<SCM2ReportBody> oneDayOneThick = oneDayList.Where(t => t.Thickness == oneCouple.Value).ToList();
                                //calcolo i totali e li aggiungo al report
                                SCM2ReportToFile oneLine = SCM2Mapper(oneDayOneThick);
                                report.Add(oneLine);
                            }
                        }
                    }

                    csvList = genPurpose.ExportObj2CsvList<SCM2ReportToFile>(report);                
                    break;
            }

            string csvContent = genPurpose.List2Csv(csvList);
            string outputFile = $"{machineName}_{startTime}_{endTime}.csv";
            byte[] bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", outputFile);                   
        }

        public SCM2ReportToFile SCM2Mapper(List<SCM2ReportBody> inputList)
        {
            TimeSpan oraInizioLavoro = inputList.Select(d => d.DateTime.TimeOfDay).Min();
            TimeSpan oraFineLavoro  = inputList.Select(df => df.DateTime.TimeOfDay).Max();
            TimeSpan totaleOreLavoro = oraFineLavoro- oraInizioLavoro;
            double totaleMinutiLavoro = Math.Round(TimeSpan.FromMinutes(totaleOreLavoro.TotalMinutes).TotalMinutes,1);
            
            double totaleMetri = Math.Round(inputList.Sum(tm => tm.Length)/1000, 1);
            double totaleMetriConsumati = Math.Round(inputList.Sum(tmc => tmc.EdgeConsumptionLH)/1000,1);
            int totalePezzi = inputList.Count();
            double spessore = inputList[0].Thickness;

            double metriAlMinuto = Math.Round(totaleMetri/totaleMinutiLavoro,1);
            double metriAllOra = Math.Round(metriAlMinuto*60,1);

            SCM2ReportToFile report = new SCM2ReportToFile(){
                Data = inputList[0].DateTime.ToString("dd/MM/yyyy"),
                OraInizioLavoro = oraInizioLavoro,
                OraFineLavoro = oraFineLavoro,
                TotaleOreLavoro = totaleOreLavoro,
                TotaleMinutiLavoro = totaleMinutiLavoro,
                TotaleMetri = totaleMetri,
                TotaleMetriBordoConsumati = totaleMetriConsumati,
                TotalePezzi = totalePezzi,
                Spessore = spessore,
                MetriAlMinuto = metriAlMinuto,
                MetriAllOra = metriAllOra
            };

            return report;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}