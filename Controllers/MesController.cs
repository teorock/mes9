using System.Collections.Generic;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.IO;
using mes.Models.ControllersConfigModels;
using Newtonsoft.Json;
using mes.Models.Services.Application;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using mes.Models.InfrastructureModels;
using System.Data.Common;
using Microsoft.AspNetCore.Builder.Extensions;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class MesController : Controller
    {
       private readonly ILogger<MesController> _logger;
       //private readonly string connectionString ="Data Source=../mesData/datasource.db";
        MesControllerConfig config = new MesControllerConfig();
        const string mesControllerConfigPath = @"c:\core\mes\ControllerConfig\MesController.json";   
        const string intranetLog=@"c:\temp\intranet.log";              
       public MesController(ILogger<MesController> logger)
       {
           _logger = logger;

            string rawConf = "";

            using (StreamReader sr = new StreamReader(mesControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<MesControllerConfig>(rawConf);  
       }


        [Route("Main")]
        public IActionResult Main()
        {
            UserData userData = GetUserData();
            Log2File(JsonConvert.SerializeObject(userData));                        
            Log2File("------------MesController");            
            return View();
        }
        
    #region ProductionStatus

        [HttpGet]
        [Route("ProductionStatus")]
        public IActionResult ProductionStatus()
        {
            List<MachineStatusPicker> lastMachinesStatus = new List<MachineStatusPicker>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            GeneralPurpose genPurpose = new GeneralPurpose();
            List<MachineStatusPicker> allMachineStatus = dbAccessor.Queryer<MachineStatusPicker>(config.LastInstantConnString,"MachineStatus");

            //devo creare una lista contenente solo lo stato più recente per ogni macchina
            List<string> allMachines = allMachineStatus.Select(x => x.MachineName).Distinct().ToList();
            List<WorklistProgressViewmodel> wlProgress = new List<WorklistProgressViewmodel>();

            int automatico = 0;
            int attesa = 0;
            int manuali = 0;
            int spente = 0;
            int emergenza = 0;

            foreach(string oneMachine in allMachines)
            {
                List<MachineStatusPicker> oneMachineStatus = allMachineStatus.Where(x => x.MachineName == oneMachine).ToList();            
                
                MachineStatusPicker last = oneMachineStatus[oneMachineStatus.Count -1];
                //last.
                lastMachinesStatus.Add(last);

                switch(last.MachineState)
                {
                    case "start - automatico":
                        automatico ++;
                        break;
                    case "connessa":
                        automatico ++;
                        break;
                    case "in attesa":
                        attesa ++;
                        break;
                    case "mov.manuali":
                        manuali++;
                        break;
                    case "non connessa":
                        spente++;
                        break;
                    case "emergenza":
                        emergenza++;
                        break;                        
                }
                if(oneMachineStatus[0].MachineType=="BIESSE1")  
                {
                    string machineName = oneMachineStatus[oneMachineStatus.Count-1].MachineName;
                    string worklistName = oneMachineStatus[oneMachineStatus.Count-1].WorklistName;
                    //TO DO: try catch, il file potrebbe non esistere

                    string totalQuantity = "0";
                    string totalCounter = "0";
                    try
                    {
                        totalQuantity = GetWorklistTotalProgress(worklistName, machineName, true).Key.ToString();
                        totalCounter =  GetWorklistTotalProgress(worklistName, machineName, true).Value.ToString();
                    }
                    catch(Exception ex)
                    {
                        Log2File($"ERRORE: MesController/ProductionStatus/GetWorklistTotalProgress: GetWorklistTotalProgress {machineName}/{worklistName}->{ex.Message}");
                    }

                    WorklistProgressViewmodel oneProgress = new WorklistProgressViewmodel()
                    {
                        
                        MachineName = machineName,
                        WorklistName = worklistName,
                        TotalQuantity = totalQuantity,
                        TotalCounter =  totalCounter                      
                    };
                    wlProgress.Add(oneProgress);
                }           
            }

            ViewBag.automatico = automatico;
            ViewBag.attesa = attesa;
            ViewBag.spente = spente;
            ViewBag.manuali = manuali;
            ViewBag.emergenza = emergenza;           

            ViewBag.allMachines = allMachines;

            ViewBag.wlProgress = wlProgress;

            //--------------
            ViewBag.startDate = genPurpose.GetWeeksMonday(0).ToString("dd/MM/yyyy");
            ViewBag.endDate = genPurpose.GetWeeksMonday(5).ToString("dd/MM/yyyy");

            return View("ProductionStatus", lastMachinesStatus); 
        }

        [HttpGet]
        [Route("GetMachineHistory")]
        public IActionResult GetMachineHistory(string machineName, string startDate, string endDate)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            GeneralPurpose genPurpose = new GeneralPurpose();
            
            //date a caso per sviluppo

            //startDate = genPurpose.GetWeeksMonday(0).ToString("dd/MM/yyyy");
            //endDate = genPurpose.GetWeeksMonday(5).ToString("dd/MM/yyyy");

            DateTime start = Convert.ToDateTime(startDate);
            DateTime end = Convert.ToDateTime(endDate);

            string filter = $"MachineName=\'{machineName}\'";
            List<MachineStatusPicker> allMachineStatus = dbAccessor.QueryerFilter<MachineStatusPicker>(config.LastPeriodConnString,"MachineStatus", filter);

            var debug = allMachineStatus.Last();

            List<MachineStatusPicker> oneMachineStatus = allMachineStatus.Where(d => Convert.ToDateTime(d.Date) >= start.Date & Convert.ToDateTime(d.Date) <= end.Date).ToList();
           
            MachineStatusPicker last = oneMachineStatus.Last();
          
            List<string> allMachines = dbAccessor.Queryer<MacchineListModel>(config.LastPeriodConnString,"Macchine").Select(n => n.MachineName).ToList();

            ViewBag.avanzamento = CalcolaAvanzamento(last.Counter, last.Quantity);
            ViewBag.allMachines = allMachines;

            //calcolo percentuali
            double automatico = oneMachineStatus.Where(x => x.MachineState == "start - automatico").Count() 
                            + oneMachineStatus.Where(y => y.MachineState =="connessa").Count()
                            + oneMachineStatus.Where(z => z.MachineState == "in lavorazione").Count();

            double attesa = oneMachineStatus.Where(z => z.MachineState == "in attesa").Count();
            double manuali = oneMachineStatus.Where(k => k.MachineState == "mov.manuali").Count();
            double scollegata = oneMachineStatus.Where(w => w.MachineState =="non collegata").Count();

            string dataInizio = oneMachineStatus[0].Date;
            string dataFine = oneMachineStatus[oneMachineStatus.Count()-1].Date;

            int totalMachineStatus = oneMachineStatus.Count();

            ViewBag.automatico = Math.Round((automatico/totalMachineStatus*100),1);
            ViewBag.attesa = Math.Round((attesa/totalMachineStatus*100),1);
            ViewBag.manuali = Math.Round((manuali/totalMachineStatus*100),1);
            ViewBag.scollegata = Math.Round((scollegata/totalMachineStatus*100),1);
            ViewBag.dataInizio = dataInizio;
            ViewBag.dataFine = dataFine;

            //----------------- date per i calendari
            ViewBag.calendarStartDate = Convert.ToDateTime(dataInizio).ToString("yyyy-MM-dd");
            ViewBag.calendarEndDate = Convert.ToDateTime(dataFine).ToString("yyyy-MM-dd");

            List<MachineStatustPickerWeek> oneWeekStatus = WeekPeeker(oneMachineStatus, start, end);
            ViewBag.oneWeekStatus = oneWeekStatus;
            //preleva una lista degli ultimi 5 movimenti di MachineMovements
            ViewBag.oneWeekProgs = GetLastWeekProgs(last.MachineName, 7);
                                    
            return View("GetMachineHistory", last);
        }

        public IActionResult WLDetails(string wldata)
        {
            //macchina-distinta-programma
            string[] parts = wldata.Split('-');

            string localFile = $"{Path.Combine(config.FtpLocalDestination, parts[1] + ".wlist")}";
            GetFtpWorklist(config.FtpServer, config.FtpUser, parts[1], parts[0], localFile);
                        
            WorklistService wlService = new WorklistService();
            List<WorklistCounter> model = wlService.GetWorklistContent(localFile);

            List<int> quantities = model.Select(q => Convert.ToInt32(q.Quantity)).ToList();
            List<int> counters = model.Select(c => Convert.ToInt32(c.Counter)).ToList();

            if(parts[2]!=null) ViewBag.actualProgram = parts[2];
            ViewBag.machineName = parts[0];
            ViewBag.worklistName = parts[1];
            ViewBag.totalQuantity = quantities.Take(quantities.Count()).Sum();
            ViewBag.totalCounter = counters.Take(counters.Count()).Sum();

            return View(model);
        }

        private void GetFtpWorklist(string server, string userName, string worklistName, string machineName, string localFile)
        {
            GeneralPurpose genP = new GeneralPurpose();
            FtpService ftpService = new FtpService(server, userName, genP.ImplicitPwd(userName));
            string remoteFile = $"/{machineName}/{worklistName}.wlist";
            ftpService.FtpDownloadFile(remoteFile,localFile );            
        }

        private KeyValuePair<int,int> GetWorklistTotalProgress(string worklistName, string machineName, bool remote)
        {
            //KeyValuePair<int, int> totalProgress = new KeyValuePair<int, int>();

            string localFile = $"{Path.Combine(config.FtpLocalDestination, worklistName + ".wlist")}"; 
            
            if(remote) GetFtpWorklist(config.FtpServer,config.FtpUser, worklistName, machineName, localFile);
            
            WorklistService wlService = new WorklistService();
            List<WorklistCounter> model = wlService.GetWorklistContent(localFile);
            
            List<int> quantities = model.Select(q => Convert.ToInt32(q.Quantity)).ToList();
            List<int> counters = model.Select(c => Convert.ToInt32(c.Counter)).ToList();
            int totalQuantity = quantities.Take(quantities.Count()).Sum();
            int totalCounter = counters.Take(counters.Count()).Sum();
            
            KeyValuePair<int,int> totalProgress = new KeyValuePair<int, int>(totalQuantity, totalCounter);

            return totalProgress;
        }

        private List<KeyValuePair<string,string>> GetLastWeekProgs(string machineName, int range)
        {            
            List<KeyValuePair<string,string>> result = new List<KeyValuePair<string, string>>();

            //TO DO: passare allMachineMovements direttamente

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MachineMovementsPicker> allMachineMovements = dbAccessor.Queryer<MachineMovementsPicker>(config.ConnectionString, "MachineMovements")
                                                                .Where(x=> x.MachineName == machineName).ToList();
            if(allMachineMovements.Count == 0) return result;

            int start = allMachineMovements.Count-1;
            int stop = start - range;
            for(int x=start; x>0; x--)
            {
                KeyValuePair<string,string> test = new KeyValuePair<string, string>(allMachineMovements[x].WorklistName, allMachineMovements[x].ProgramName);
                if(!result.Contains(test)) result.Add(test);
                if(result.Count == range) break;
            }
            return result;
        }

        public  List<DateTime> GetDaysInBetween(DateTime startDate, DateTime endDate)
        {
            //le date devono essere sempre 7
            List<DateTime> allDates = new List<DateTime>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                allDates.Add(date.Date);
            }

            allDates.Reverse();
            return allDates;
        }

        //public List<DateTime> GetDaysInBetween(DateTime startDate, DateTime endDate)
        //{
        //    List<DateTime> allDates = new List<DateTime>();
        //    DateTime currentDate = startDate.Date;  
//
        //    while (currentDate <= endDate.Date) 
        //    {
        //        allDates.Add(currentDate);
        //        currentDate = currentDate.AddDays(1);
        //    }
//
        //    if (allDates.Count < 7)
        //    {
        //        currentDate = endDate.Date.AddDays(1); 
//
        //        while (allDates.Count < 7)
        //        {
        //            allDates.Add(currentDate);
        //            currentDate = currentDate.AddDays(1);
        //        }
        //    }
//
        //    if(allDates.Count > 7)
        //    {
        //        allDates = allDates.Take(7).ToList();
        //    }
//
        //    allDates.Reverse();
        //    return allDates;
        //}

        private List<MachineStatustPickerWeek> WeekPeeker(List<MachineStatusPicker> oneMachineStatus, DateTime startDate, DateTime endDate)
        {
            //calcolo tutti i giorni tra StartDate e EndDate
            //per ogni giorni nella lista controllo oneMachineStatus
            //se la data è presente creo un oggetto con i dati
            //se la data non c'è creo un oggetto con valori a zero tranne la data

            List<MachineStatustPickerWeek> weekPeek = new List<MachineStatustPickerWeek>();

            List<DateTime> allDatesInBetween = GetDaysInBetween(startDate, endDate);

            foreach(DateTime oneDate in allDatesInBetween)
            {
                MachineStatusPicker oneDayStatus = oneMachineStatus.Where(d => Convert.ToDateTime(d.Date) == oneDate).FirstOrDefault();
                if(oneDayStatus != null)
                {
					int automatico = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "start - automatico").Count()
													+ oneMachineStatus.Where(z => z.MachineState == "in lavorazione").Count() ;

					int connessa = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "connessa").Count();

					int attesa = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "in attesa").Count();

					int manuali = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "mov.manuali").Count();

					int spenta = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "non connessa").Count();

					int nonconnessa = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "non connessa").Count(); 

					int emergenza = oneMachineStatus.Where(x => Convert.ToDateTime(x.Date).Date == oneDate)
													 .Where(n => n.MachineState == "emergenza").Count();

					//int dayTotal = oneDaysStatus.Where(d => d.Date== allDates[i]).Count();
					int dayTotal = automatico + connessa + attesa + manuali + spenta + nonconnessa + emergenza;

					MachineStatustPickerWeek onePeek = new MachineStatustPickerWeek() 
					{
						Day = oneDate.Date.ToString("dd/MM/yyyy"),
						Start = CalcolaPercentuale(automatico, dayTotal).ToString(),
						Waiting = CalcolaPercentuale(attesa,dayTotal).ToString(),
						ManualMovements = CalcolaPercentuale(manuali, dayTotal).ToString(),
						Connected = CalcolaPercentuale(connessa, dayTotal).ToString(),
						NotConnected = CalcolaPercentuale(nonconnessa, dayTotal).ToString(),
						Emergency = CalcolaPercentuale(emergenza, dayTotal).ToString()
					};

					weekPeek.Add(onePeek);
                }
                else
                {
					MachineStatustPickerWeek onePeek = new MachineStatustPickerWeek() 
					{
						Day = oneDate.Date.ToString("dd/MM/yyyy"),
						Start = "0",
						Waiting = "0",
						ManualMovements = "0",
						Connected = "0",
						NotConnected = "0",
						Emergency = "0"
					};

					weekPeek.Add(onePeek);
                }
            }


            return weekPeek;
        }

        private List<MachineStatustPickerWeek> GetEmptyWeek()
        {
            List<MachineStatustPickerWeek> emptyWeek = new List<MachineStatustPickerWeek>();
            for(int i=0; i<7; i++)
            {
                emptyWeek.Add(new MachineStatustPickerWeek()
                {
                    Day= "01/01/2020",
                    Start = "0",
                    Waiting ="0",
                    ManualMovements = "0",
                    Connected = "0",
                    NotConnected = "0",
                    Emergency ="0"
                });
            }
            return emptyWeek;
        }

        private int CalcolaAvanzamento(string counter, string quantity)
        {
            int barWidth=0;
            try
            {
                double count = Convert.ToDouble(counter);
                double quant = Convert.ToDouble(quantity);
                barWidth = Convert.ToInt16(Math.Round((count/quant)*100,0));
            }
            catch(Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
                return barWidth;
            }
            
            return barWidth;
        }

        private int CalcolaPercentuale(int counter, int quantity)
        {
            int barWidth=0;
            try
            {
                double count = Convert.ToDouble(counter);
                double quant = Convert.ToDouble(quantity);
                barWidth = Convert.ToInt16(Math.Round((count/quant)*100,0));
            }
            catch(Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
                return barWidth;
            }
            
            return barWidth;
        }        

    #endregion

        private UserData GetUserData()
        {
            UserData userData = new UserData();

            string userRoles="";
            ViewBag.userId =  User.FindFirstValue(ClaimTypes.NameIdentifier); // will give the user's userId
            userData.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.userName =  User.FindFirstValue(ClaimTypes.Name); // will give the user's userName
            userData.UserName = User.FindFirstValue(ClaimTypes.Name);

            IEnumerable<Claim> roles = User.FindAll(ClaimTypes.Role);
            foreach(var role in roles)
            {
                userRoles += $"{role.Value}, ";
            }
            userData.UserRoles = userRoles;
            
            ViewBag.userEmail =  User.FindFirstValue(ClaimTypes.Email); // will give the user's Email
            userData.UserEmail = User.FindFirstValue(ClaimTypes.Email);

            ViewBag.userRoles= userRoles;                    

            //ViewBag.address = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString().Substring(7);
            ViewBag.address = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            ViewBag.port = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            //userData.UserIpAddress = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString().Substring(7);
            userData.UserIpAddress = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            userData.UserIpPort = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            return userData;
        }

        private void Log2File(string line2log)
        {
            using(StreamWriter sw = new StreamWriter(intranetLog, true))
            {
                sw.WriteLine($"{DateTime.Now} -> {line2log}");
            }
        }     

    }
}