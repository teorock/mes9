using System.Collections.Generic;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;

namespace mes.Controllers
{
    [Route("[controller]")]
    public class MesController : Controller
    {
       private readonly ILogger<MesController> _logger;
       private readonly string connectionString ="Data Source=../mesData/datasource.db";
       public MesController(ILogger<MesController> logger)
       {
           _logger = logger;
       }


        [Route("Main")]
        public IActionResult Main()
        {
            return View();
        }
        
        [HttpGet]
        [Route("ProductionStatus")]
        public IActionResult ProductionStatus()
        {
            List<MachineStatusPicker> lastMachinesStatus = new List<MachineStatusPicker>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MachineStatusPicker> allMachineStatus = dbAccessor.Queryer<MachineStatusPicker>(connectionString,"MachineStatus");

            //devo creare una lista contenente solo lo stato più recente per ogni macchina
            List<string> allMachines = allMachineStatus.Select(x => x.MachineName).Distinct().ToList();

            int automatico = 0;
            int attesa = 0;
            int manuali = 0;
            int spente = 0;
            int emergenza = 0;

            foreach(string oneMachine in allMachines)
            {
                List<MachineStatusPicker> oneMachineStatus = allMachineStatus.Where(x => x.MachineName == oneMachine).ToList();
                
                //elimino per velocizzare
                //List<long>allIds = oneMachineStatus.Select(y => y.id).ToList();
                //MachineStatusPicker last = oneMachineStatus.Where(z => z.id == allIds.Max()).FirstOrDefault();
                
                MachineStatusPicker last = oneMachineStatus[oneMachineStatus.Count -1];
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
            }

            ViewBag.automatico = automatico;
            ViewBag.attesa = attesa;
            ViewBag.spente = spente;
            ViewBag.manuali = manuali;
            ViewBag.emergenza = emergenza;           

            ViewBag.allMachines = allMachines;
            return View("ProductionStatus", lastMachinesStatus); 
        }

        [HttpGet]
        [Route("GetMachineHistory")]
        public IActionResult GetMachineHistory(string machineName)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MachineStatusPicker> allMachineStatus = dbAccessor.Queryer<MachineStatusPicker>(connectionString,"MachineStatus");

            List<MachineStatusPicker> oneMachineStatus = allMachineStatus.Where(x => x.MachineName == machineName).ToList();                    
            MachineStatusPicker last = oneMachineStatus[oneMachineStatus.Count -1];            

            //devo creare una lista contenente solo lo stato più recente per ogni macchina
            List<string> allMachines = allMachineStatus.Select(x => x.MachineName).Distinct().ToList();            

            ViewBag.avanzamento = CalcolaAvanzamento(last.Counter, last.Quantity);
            ViewBag.allMachines = allMachines;

            //calcolo percentuali
            //100 = oneMAchineStatus.count
            double automatico = oneMachineStatus.Where(x => x.MachineState == "start - automatico").Count() 
                            + oneMachineStatus.Where(y => y.MachineState =="connessa").Count()
                            + oneMachineStatus.Where(z => z.MachineState == "in lavorazione").Count();

            double attesa = oneMachineStatus.Where(z => z.MachineState == "in attesa").Count();
            double manuali = oneMachineStatus.Where(k => k.MachineState == "mov.manuali").Count();
            double scollegata = oneMachineStatus.Where(w => w.MachineState =="non collegata").Count();

            string dataInizio = oneMachineStatus[0].Date;
            string dataFine = oneMachineStatus[oneMachineStatus.Count()-1].Date;

            ViewBag.automatico = Math.Round((automatico/oneMachineStatus.Count()*100),0);
            ViewBag.attesa = Math.Round((attesa/oneMachineStatus.Count()*100),0);
            ViewBag.manuali = Math.Round((manuali/oneMachineStatus.Count()*100),0);
            ViewBag.scollegata = Math.Round((scollegata/oneMachineStatus.Count()*100),0);
            ViewBag.dataInizio = dataInizio;
            ViewBag.dataFine = dataFine;

            List<MachineStatustPickerWeek> oneWeekStatus = WeekPeeker(oneMachineStatus);
            ViewBag.oneWeekStatus = oneWeekStatus;
            //preleva una lista degli ultimi 5 movimenti di MachineMovements
            ViewBag.oneWeekProgs = GetLastWeekProgs(last.MachineName, 7);
            //ultimi 7gg accesa spenta?
            //ultimi 5 movements?
                                    
            return View("GetMachineHistory", last);
        }

        private List<KeyValuePair<string,string>> GetLastWeekProgs(string machineName, int range)
        {            
            List<KeyValuePair<string,string>> result = new List<KeyValuePair<string, string>>();

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MachineMovementsPicker> allMachineMovements = dbAccessor.Queryer<MachineMovementsPicker>(connectionString, "MachineMovements")
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


        private List<MachineStatustPickerWeek> WeekPeeker(List<MachineStatusPicker> oneMachineStatus)
        {
            List<MachineStatustPickerWeek> weekPeek = new List<MachineStatustPickerWeek>();
            List<string> allDates = oneMachineStatus.Select(x => x.Date).Distinct().ToList();

            int total = allDates.Count;
            if(total <7) return GetEmptyWeek();

            int range = 7;

            for(int i= total-1; i>total-range-1; i--)
            {
                int automatico = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "start - automatico").Count()
                                + oneMachineStatus.Where( y => y.MachineState == "connessa").Count()
                                + oneMachineStatus.Where(z => z.MachineState == "in lavorazione").Count() ;

                int connessa = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "connessa").Count();

                int attesa = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "in attesa").Count();

                int manuali = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "mov.manuali").Count();

                int spenta = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "non connessa").Count();

                int nonconnessa = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "non connessa").Count(); 

                int emergenza = oneMachineStatus.Where(x => x.Date == allDates[i])
                                                 .Where(n => n.MachineState == "emergenza").Count();

                int dayTotal = oneMachineStatus.Where(d => d.Date== allDates[i]).Count();

                MachineStatustPickerWeek onePeek = new MachineStatustPickerWeek() 
                {
                    Day = allDates[i],
                    Start = CalcolaPercentuale(automatico, dayTotal).ToString(),
                    Waiting = CalcolaPercentuale(attesa,dayTotal).ToString(),
                    ManualMovements = CalcolaPercentuale(manuali, dayTotal).ToString(),
                    Connected = CalcolaPercentuale(connessa, dayTotal).ToString(),
                    NotConnected = CalcolaPercentuale(nonconnessa, dayTotal).ToString(),
                    Emergency = CalcolaPercentuale(emergenza, dayTotal).ToString()
                };

                weekPeek.Add(onePeek);
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
                return barWidth;
            }
            
            return barWidth;
        }        

        private void GetMachineStatus(string machineName, string machineIp, string getProtocol)
        {
            // in base al getProtocol e al machineName interroga l'ip
            // serve un file di configurazione macchina, ip, sistema di prelievo delle info
            //protocolli
            //pcquo
            //json-1 (Akron)
            //
        }

    }
}