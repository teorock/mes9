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
using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Roles ="root, PfcAggiorna, PfcCrea, PfcLeggi")]
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

            ViewBag.userRoles = userData.UserRoles;

            return View(allPfc);
        }

        [HttpGet]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public IActionResult InsertPfc()
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");            
            //-------------------------);
            ViewBag.userRoles = userData.UserRoles;

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
            operators.Insert(0, "-----");
            ViewBag.Operators = operators;
            

            List<PfcModel> allPfc = dbAccessor.Queryer<PfcModel>(config.ConnString2, config.PfcTable).ToList();
            int pfcPresenti = allPfc.Count();
            
            ViewBag.allPfc = allPfc;
            ViewBag.nCommessa = pfcPresenti +1;
            ViewBag.nCommessaTitle = $"{pfcPresenti + 1}/{DateTime.Now.ToString("yyyy")}";
                
            return View();
        }

        [HttpPost]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
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
                    CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };

                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                var result = dbAccessor.Insertor<PfcModel>(config.ConnString2, config.PfcTable, pcf2insert);
                if(result == 1) return RedirectToAction("Error");
            }
            return RedirectToAction("Index");
        }

        //===================================================================

        [HttpGet]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public IActionResult ModPfc(long inputId)
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");            
            //-------------------------);
            ViewBag.userRoles = userData.UserRoles;


            // Get the record to modify
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            var pfc = dbAccessor.Queryer<PfcModel>(config.ConnString2, config.PfcTable)
                .Where(i => i.id == inputId)
                .FirstOrDefault();

            if (pfc == null)
            {
                return NotFound();
            }

            // Prepare the view model from the PfcModel
            var viewModel = new WorkorderViewModel
            {
                id = pfc.id,
                WorkNumber = pfc.NumeroCommessa,
                Customer = pfc.Cliente,
                ExternalRef = pfc.RifEsterno,
                Delivery = DateTime.Parse(pfc.DataConsegna),
                Description = pfc.Descrizione, 
                // Convert LavorazioniJsonString to WorkPhases collection
                WorkPhases = JsonConvert.DeserializeObject<List<WorkphaseViewModel>>(pfc.LavorazioniJsonString)
            };

            // Populate ViewBag with necessary data
            ViewBag.nCommessa = pfc.id;
            ViewBag.nCommessaTitle = pfc.NumeroCommessa;
            ViewBag.Customers = GetCustomersList(); 
            ViewBag.WorkPhases = GetWorkPhasesList(); 
            ViewBag.Operators = GetOperatorsList(); 
            //ViewBag.Works = GetExistingWorks(); 

            return View(viewModel);
        }

        //===================================================================

        [HttpPost]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public IActionResult ModPfc(WorkorderViewModel model)
        {
            if (ModelState.IsValid)
            {
                //Ottengo l'utente connesso
                UserData userData = GetUserData();
                //--------------------------
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
                Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");

                string jsonLavorazioni = JsonConvert.SerializeObject(model.WorkPhases);

                PfcModel pcf2update = new PfcModel() {
                    id = model.id,
                    NumeroCommessa = model.WorkNumber,
                    Cliente = model.Customer,
                    Descrizione = model.Description,
                    RifEsterno = model.ExternalRef,
                    DataConsegna = Convert.ToDateTime(model.Delivery).ToString("dd/MM/yyyy HH:mm"),
                    LavorazioniJsonString = jsonLavorazioni,
                    Enabled = "1",
                    CreatedBy = userData.UserName,
                    CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm") 
                };

                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                var result = dbAccessor.Updater<PfcModel>(config.ConnString2, config.PfcTable, pcf2update, model.id);

                return RedirectToAction("Index"); 
            }
            else
            {
                // If the model is not valid, return the view with the model
                // Populate ViewBag with necessary data
                ViewBag.nCommessa = model.id;
                ViewBag.nCommessaTitle = model.WorkNumber;
                ViewBag.Customers = GetCustomersList(); 
                ViewBag.WorkPhases = GetWorkPhasesList(); 
                ViewBag.Operators = GetOperatorsList(); 
                //ViewBag.Works = GetExistingWorks(); 
                return View(model);
            }
        }

        //===================================================================

        [HttpGet]
        public IActionResult LoadCsv(string filename)
        {
            return RedirectToAction("Index");
        }

        private List<string>GetCustomersList()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<string> clienti = dbAccessor.Queryer<ClienteViewModel>(config.ConnString2, config.CustomerTable)
                                                        .Where(e => e.Enabled =="1")
                                                        .Select(c =>c.Nome).ToList();

            return clienti;
        }

        private List<string>GetWorkPhasesList()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<LavorazioneViewModel> lavorazioni = dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString2, config.WorkphaseTable);
            List<string> works = lavorazioni.Select(n => n.NomeLavorazione).ToList();    

            return works;      
        }

        private List<string> GetOperatorsList()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(config.ConnString, config.OperatorsTable)
                                        .Where(e => e.Enabled == "1")
                                        .Where(ap => ap.EnabledProduzione == "1").ToList();
            List<string>operators = dipendenti.Select(op => $"{op.Nome} {op.Cognome}").ToList();
            operators.Insert(0, "-----");
            return operators;
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