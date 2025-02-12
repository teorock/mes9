using System;
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
using System.IO;
using mes.Models.ControllersConfigModels;
using Microsoft.Extensions.Logging;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class Totem : Controller
    {
        private readonly ILogger<Totem> _logger;

        //TotemControllerConfig config = new TotemControllerConfig();
        const string totemControllerConfigPath = @"c:\core\mes\ControllerConfig\TotemController.json";
        //string authorized = "";
        //const string intranetLog=@"c:\temp\intranet.log";   

        public Totem(ILogger<Totem> logger)
        {
            _logger = logger;
            //string rawConf = "";
//
            //using (StreamReader sr = new StreamReader(totemControllerConfigPath))
            //{
            //    rawConf = sr.ReadToEnd();
            //}
            //config = JsonConvert.DeserializeObject<TestControllerConfig>(rawConf);
             
        }

        public IActionResult Index(int totemnumber, int weekNumber)
        {
            //facciamo un file di configurazione per eventuali futuri totem
            // il file contiene una lista di stringhe di filtro associate al numero id del totem da visualizzare

            //che settimana è se weekNumber è nullo
            // estrai i dati di quella settimana
            //e li filtra in base 
            return View();
        }

        public string GetTotemEvents(string startDate, string endDate, List<string> filters)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = new List<ProductionCalendarDbModel>();

            //for(int i=0; i< filters.Length; i++)
            //{
            //    allEvents.AddRange(dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
            //                                                    .Where(n => n.assignedTo == filters[i])
            //                                                    .Where(x => x.enabled =="1").ToList());
            //}
            
            var result = JsonConvert.SerializeObject(allEvents);

            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}