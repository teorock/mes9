using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mes.Models.ViewModels;
using mes.Models.Services.Infrastructures;
using System.Linq;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using mes.Models.ControllersConfigModels;
using ServiceStack;

namespace intranet.Controllers
{
    //[Route("[controller]")]
    public class DataInputController : Controller
    {
        private readonly ILogger<DataInputController> _logger;
        const string datainputControllerConfigPath = @"c:\core\mes\ControllerConfig\DataInputControllerConfig.json";
        DataInputControllerConfig config = new DataInputControllerConfig();
        private readonly UserManager<IdentityUser> _userManager;  
        const string intranetLog = @"c:\temp\intranet.log";

        public DataInputController(ILogger<DataInputController> logger)
        {
            _logger = logger;

            string rawConf = "";

            using (StreamReader sr = new StreamReader(datainputControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<DataInputControllerConfig>(rawConf); 

        }
      
        public IActionResult AnagraficheMain()
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------               
            return View();
        }

    #region clienti

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi, AnagraficheLeggi")]
        public IActionResult Customers()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> allCustomers = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(config.ConnString, config.CustomersDbTable);
                        
            return View("Customers", allCustomers);
        }
        
        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult ModCustomer(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(config.ConnString, config.CustomersDbTable); 
            
            ViewBag.customersList = clienti;
            ClienteViewModel oneModel = clienti.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult ModCustomer(ClienteViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<ClienteViewModel>(config.ConnString, config.CustomersDbTable, oneModel, oneModel.id);

            return RedirectToAction("Customers");
        }        

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult InsertCustomer()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(config.ConnString, config.CustomersDbTable);            
            
            ViewBag.CustomersList = clienti;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult InsertCustomer(ClienteViewModel newCustomer)
        {            
            UserData userData = GetUserData();

            newCustomer.CreatedBy = userData.UserName;
            newCustomer.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");          

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(config.ConnString, config.CustomersDbTable);      

            long max = (from l in clienti select l.id).Max();

            newCustomer.id = max + 1;
            GeneralPurpose genPurp = new GeneralPurpose();

            int result = dbAccessor.Insertor<ClienteViewModel>(config.ConnString, config.CustomersDbTable, genPurp.DenullifyObj<ClienteViewModel>(newCustomer));

            return RedirectToAction("Customers");
        }


    #endregion

 
    #region Articoli

        [HttpGet]
        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult MainArticoli()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ArticoloViewModel> Articoli = dbAccessor.Queryer<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable)
                                                        .Where(x => x.Enabled =="1").ToList();            
            
            return View(Articoli);
        }

        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult AggiornaArticoli(List<ArticoloViewModel> Articoli)
        {
                UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------                
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(ArticoloViewModel oneModel in Articoli)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    int result = dbAccessor.Updater<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable, oneModel, oneModel.id);
                }
            return RedirectToAction("MainArticoli");
        }

        [HttpGet]
        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult InsertArticolo()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ArticoloViewModel> Articoli = dbAccessor.Queryer<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.ArticoliList = Articoli;
            ViewBag.customers = GetCustomers();
            ViewBag.panels = GetPanelsCodes();
            ViewBag.semilavs = GetSemilavCodes();

            ViewBag.articoliEsistenti = Articoli.Select(a => a.Codice).ToList();            

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult InsertArticolo(ArticoloViewModel newArticolo)
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            newArticolo.CreatedBy = userData.UserName;
            newArticolo.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newArticolo.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ArticoloViewModel> Articoli = (List<ArticoloViewModel>)dbAccessor.Queryer<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable);

            //verifica se Articoli = null
            long max = (from l in Articoli select l.id).Max();

            newArticolo.id = max + 1;

            int result = dbAccessor.Insertor<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable, newArticolo);

            return RedirectToAction("MainArticoli");
        }

        [HttpGet]
        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult ModArticolo(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ArticoloViewModel> Articoli = (List<ArticoloViewModel>)dbAccessor.Queryer<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.ArticoliList = Articoli;
            
            ArticoloViewModel oneModel = Articoli.Where(x => x.id == id).FirstOrDefault();

            ViewBag.customers = GetCustomers();
            ViewBag.panelsCodes = GetPanelsCodes();
            ViewBag.semilavs = GetSemilavCodes();
            
            //indici di selezione delle tendine
            ViewBag.selectedCustomer = GetCustomers().IndexOf(oneModel.Cliente);
            ViewBag.selectedPanel = GetPanelsCodes().IndexOf(oneModel.CodPannello);
            ViewBag.selectedSemilav = GetSemilavCodes().IndexOf(oneModel.CodSemilavorato);
        
            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, ArticoliScrivi, ArticoliLeggi")]
        public IActionResult ModArticolo(ArticoloViewModel oneModel)
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<ArticoloViewModel>(config.ConnString, config.ArticlesDbTable, oneModel, oneModel.id);

            return RedirectToAction("MainArticoli");
        }        

        private List<string> GetCustomers()
        {
            List<string> result = new List<string>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            result = dbAccessor.Queryer<ClienteViewModel>(config.ConnString, config.CustomersDbTable)
                                .Where(c => c.Enabled=="1")
                                .Select(n => n.Nome).ToList();

            return result;
        }

        private List<string> GetPanelsCodes()
        {
            List<string> result = new List<string>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            result = dbAccessor.Queryer<PannelloViewModel>(config.PanelsConnString, config.PanelsTable)
                                .Where(p => p.Enabled == "1")
                                .Select(c => c.Codice).ToList();

            return result;
        }

        private List<string> GetSemilavCodes()
        {
            List<string> result = new List<string>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            result = dbAccessor.Queryer<SemilavoratoViewModel>(config.SemilavConnString, config.SemilavDbTable)
                                .Where(p => p.Enabled == "1")
                                .Select(c => c.Codice)
                                .ToList();

            return result;
        }        

    #endregion

    #region Ordini
        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult Orders()
        {
            //commesse o ordini
            return View("Orders");
        }
    #endregion

    #region Materiali
        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult Materials()
        {
            //materiali
            return View("Materials");
        }    
    #endregion

    #region Lavorazioni

        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult MainLavorazioni()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;  
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------      

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<LavorazioneViewModel> lavorazioni = dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable)
                                                                .Where(e => e.Enabled == "1").ToList();
            //ViewBag.allLav = lavorazioni;

            return View(lavorazioni);
        }            
        
        [Authorize(Roles = "root")]
        public IActionResult AggiornaLavorazioni(List<LavorazioneViewModel> Lavorazioni)
        {
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(LavorazioneViewModel oneModel in Lavorazioni)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    int result = dbAccessor.Updater<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable, oneModel, oneModel.id);
                }
            return RedirectToAction("MainLavorazioni");
        }

        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult InsertLavorazione()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<LavorazioneViewModel> Lavorazioni = dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString, "Lavorazioni")
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.LavorazioniList = Lavorazioni;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult InsertLavorazione(LavorazioneViewModel newLavorazione)
        {
            UserData userData = GetUserData();

            newLavorazione.CreatedBy = userData.UserName;
            newLavorazione.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newLavorazione.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<LavorazioneViewModel> Lavorazioni = dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable);

            //long max = (from l in Lavorazioni select l.id).Max();
            if(Lavorazioni.Count() > 0)
            {
                long max = (from l in Lavorazioni select l.id).Max();
                newLavorazione.id = max + 1;
            } else {
                newLavorazione.id = 0;
            }

            //newLavorazione.id = max + 1;

            int result = dbAccessor.Insertor<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable, newLavorazione);

            return RedirectToAction("MainLavorazioni");
        }

        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult ModLavorazione(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<LavorazioneViewModel> Lavorazioni = (List<LavorazioneViewModel>)dbAccessor.Queryer<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.LavorazioniList = Lavorazioni;
            LavorazioneViewModel oneModel = Lavorazioni.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult ModLavorazione(LavorazioneViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<LavorazioneViewModel>(config.ConnString, config.LavorazioniDbTable, oneModel, oneModel.id);

            return RedirectToAction("MainLavorazioni");
        }        


    #endregion



        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult AssignTo()
        {
            //assegnato a macchina, persona o processo
            return View("AssignTo");
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