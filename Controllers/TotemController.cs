using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using mes.Models.InfrastructureModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using mes.Models.ControllersConfig;
using mes.Models.ControllersConfigModels;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class Totem : Controller
    {
        private readonly ILogger<Totem> _logger;

        TotemControllerConfig config = new TotemControllerConfig();
        const string totemControllerConfigPath = @"c:\core\mes\ControllerConfig\TotemControllerConfig.json";
        const string intranetLog = @"c:\temp\intranet.log";   

        public Totem(ILogger<Totem> logger)
        {
            _logger = logger;
            string rawConf = "";

            using (StreamReader sr = new StreamReader(totemControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TotemControllerConfig>(rawConf);
             
        }

        public IActionResult Index(int totemnumber, int weekNumber)
        {
            //facciamo un file di configurazione per eventuali futuri totem
            // il file contiene una lista di stringhe di filtro associate al numero id del totem da visualizzare

            //che settimana è se weekNumber è nullo
            // estrai i dati di quella settimana
            //e li filtra in base 

            GeneralPurpose genPurpose = new GeneralPurpose();
            DateTime thisWeekMonday = genPurpose.GetWeeksMonday(0);

            //ProductionCalendarDbModel ha le proprietà start e end definite come stringhe e non come DayTime
            //refactoring troppo lungo, workaround trasformando la settimana da visualizzare in stringhe
            //così si rende possibile l'estrazione tramite linq
            List<string> stringifiedWeek = StringifyTimePeriod(thisWeekMonday, 5, "yyyy-MM-dd");

            //da qui estraggo tutti gli eventi nel modello ProductionCalendarDbModel che mi interessano
            List<TotemBootstrapModel> weeksIncoming = GetProductionCalendarEvents(config.TotemEvents[0], stringifiedWeek);
            List<TotemBootstrapModel> weeksOutgoing = GetProductionCalendarEvents(config.TotemEvents[1], stringifiedWeek);
            List<TotemBootstrapModel> weeksIntervention = GetProductionCalendarEvents(config.TotemEvents[2], stringifiedWeek);

            ViewBag.titleWeek = GetWeekTitle();
            //voglio passare tre models, quindi vado di ViewBag
            ViewBag.incomingsWeek = weeksIncoming;
            ViewBag.outgoingsWeek = weeksOutgoing;
            ViewBag.interventionsWeek = weeksIntervention;
            string autoscr = (config.AutoScroll)? "true" : "false";
            ViewBag.autoScroll = autoscr;
            

            return View();
        }

        public IActionResult HideTask(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            try
            {
                string filter = $"WHERE id='{id}'";
                ProductionCalendarDbModel oneToHide = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                        .Where(i => i.id == id)
                                                        .FirstOrDefault();
                oneToHide.displayTotem = "0";

                var result = dbAccessor.Updater<ProductionCalendarDbModel>(config.ConnString, config.Table, oneToHide, id);
            }
            catch (Exception ex)
            {
                
            }
            return RedirectToAction("Index");
        }

        private string GetWeekTitle()
        {
            GeneralPurpose genP = new GeneralPurpose();
            string startWeekDay = genP.GetWeeksMonday(0).Day.ToString();
            string endWeekDay = genP.GetWeeksMonday(5).Day.ToString();

            CultureInfo it = new CultureInfo("it-IT");
            var month = DateTime.Now.ToString("MMMM", it);

            return $"dal {startWeekDay} al {endWeekDay} {month}";
        }

        private TotemBootstrapModel ProductionCalendarDbModel2TotemBootstrapModel(ProductionCalendarDbModel inputModel, string referenceDate)
        {

            TotemBootstrapModel oneTotem = new TotemBootstrapModel(){
                Title = inputModel.title,
                StartHour = inputModel.start,
                Description = inputModel.description,
                ReferenceDate = referenceDate
            };
            
            return oneTotem;
        }

        private string DatePotabilizer(string inputDate)
        {
            DateTime tempDate = Convert.ToDateTime(inputDate);
            string dayName = tempDate.ToString("dddd", new CultureInfo("it-IT")); // Corrected Line
            //string dayName = thisDay.ToString("dddd", new CultureInfo("it-IT"));

            return $"{dayName} {tempDate.Day}";

        }

        private List<string> StringifyTimePeriod(DateTime startDate, int daysInterval, string dateFormat)
        {
            List<string> result = new List<string>();
            
            for(int i=0; i<=daysInterval; i++)
            {
                result.Add(startDate.AddDays(i).ToString(dateFormat));
            }
            return result;
        }

        private List<TotemBootstrapModel> GetProductionCalendarEvents(string eventFilter, List<string> dates2extract)
        {
            List<TotemBootstrapModel> result = new List<TotemBootstrapModel>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> requestedEvents = new List<ProductionCalendarDbModel>();
            
            foreach(string oneDate in dates2extract)
            {
                List<ProductionCalendarDbModel> oneDayModel = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                                .Where(n => n.assignedTo == eventFilter)
                                                                .Where(s => s.start.Contains(oneDate))
                                                                .Where(d =>d.displayTotem =="1")
                                                                .Where(x => x.enabled =="1").ToList();

                TotemBootstrapModel oneTotem = new TotemBootstrapModel();
                if(oneDayModel.Count == 0)
                {                    
                    oneTotem.Title = "                 ";
                    oneTotem.StartHour = "                 ";
                    oneTotem.Description = "                 ";
                    oneTotem.ReferenceDate = DatePotabilizer(oneDate);  

                    result.Add(oneTotem);                  
                }
                else
                {
                    foreach(ProductionCalendarDbModel oneProdCal in oneDayModel)
                    {
                        oneTotem.id = oneProdCal.id;
                        oneTotem.Title = oneProdCal.title;
                        oneTotem.StartHour = oneProdCal.start;
                        oneTotem.Description = oneProdCal.description;
                        oneTotem.ReferenceDate = DatePotabilizer(oneDate);    

                        result.Add(oneTotem);
                        oneTotem = new TotemBootstrapModel();                   
                    }
                }                
            }       

            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}