using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.InfrastructureModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceStack.Text;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class PfcController : Controller
    {
        private readonly ILogger<PfcController> _logger;
        PfcControllerConfig config = new PfcControllerConfig();
        const string mesControllerConfigPath = @"c:\core\mes\ControllerConfig\PfcController.json";   
        const string intranetLog=@"c:\temp\intranet.log";           
        public PfcController(ILogger<PfcController> logger)
        {
            _logger = logger;

            string rawConf = "";

            using (StreamReader sr = new StreamReader(mesControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<PfcControllerConfig>(rawConf);              
        }

        public IActionResult Index()
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");            
            //--------------------------
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PfcModel> allPfc = dbAccessor.Queryer<PfcModel>(config.ConnString2, config.PfcTable);

            return View(allPfc);
        }

        [HttpGet]
        public IActionResult InsertPfc()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = dbAccessor.Queryer<ClienteViewModel>(config.ConnString2, config.CustomerTable)
                                                        .Where(e => e.Enabled =="1").ToList();

            List<string> customers = clienti.Select(n => n.Nome).ToList();
            ViewBag.Customers = customers;

            List<LavorazioneViewModel> lavorazioni = dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString2, config.WorkphaseTable);
            List<string> works = lavorazioni.Select(n => n.NomeLavorazione).ToList();
            ViewBag.WorkPhases = works;

            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(config.ConnString, config.OperatorsTable)
                                                    .Where(e => e.Enabled == "1")
                                                    .Where(ap => ap.EnabledProduzione == "1").ToList();
            List<string>operators = dipendenti.Select(op => $"{op.Nome} {op.Cognome}").ToList();
            ViewBag.Operators = operators;
            
            //numero di commessa disponibile
            //prendi il numero di record in config.PfcTable
            List<PfcModel> allPfc = dbAccessor.Queryer<PfcModel>(config.ConnString2, config.PfcTable).ToList();
            int pfcPresenti = allPfc.Count();
            
            ViewBag.allPfc = allPfc;
            ViewBag.nCommessa = pfcPresenti +1;
            ViewBag.nCommessaTitle = $"{pfcPresenti + 1}/{DateTime.Now.ToString("yyyy")}";
                
            return View();
        }

        [HttpPost]
        public IActionResult InsertPfc(WorkorderViewModel inputModel)
        {
            if(ModelState.IsValid)
            {
                //ottengo la stringa json delle lavorazioni
                //compongo l'oggetto
                //scrivo sul database

                UserData userData = GetUserData();
                //--------------------------
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
                Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");

                string jsonLavorazioni = JsonConvert.SerializeObject(inputModel.WorkPhases);

                PfcModel pcf2insert = new PfcModel() {
                    id=inputModel.id,
                    NumeroCommessa = inputModel.WorkNumber,
                    Cliente = inputModel.Customer,
                    RifEsterno = inputModel.ExternalRef,
                    DataConsegna = Convert.ToDateTime(inputModel.Delivery).ToString("dd/MM/yyyy HH:mm"),
                    LavorazioniJsonString = jsonLavorazioni,
                    Enabled = "1",
                    CreatedBy = userData.UserName,
                    CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm")
                };

                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                var result = dbAccessor.Insertor<PfcModel>(config.ConnString2, config.PfcTable, pcf2insert);
                if(result == 1) return RedirectToAction("Error");
            }
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

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