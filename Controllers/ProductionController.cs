using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceStack;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class ProductionController : Controller
    {
        private readonly ILogger<ProductionController> _logger;
        ProductionControllerConfig config = new ProductionControllerConfig();
        const string prodControllerConfigPath = @"c:\core\mes\ControllerConfig\ProductionController.json";
        const string intranetLog = @"c:\temp\intranet.log";

        public ProductionController(ILogger<ProductionController> logger)
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(prodControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<ProductionControllerConfig>(rawConf);                
            _logger = logger;

        }

    #region productionDashboard

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult Index()
        {

            UserData userData = GetUserData();            
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> requests = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                                .Where(e => e.Enabled == "1").ToList();

            List<ProductionRequest> evidenziati = new List<ProductionRequest>();
            foreach(var oneRequest in requests)
            {
                evidenziati.Add(ComputeAvailability(oneRequest));
            }

            //ViewBag lista macchine
            List<string> assignments = config.CalendarAssignments.Select(a => a.AssignmentName).ToList();
            ViewBag.assignments = assignments;

            return View(evidenziati);
        }


        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult AggiornaProductionRequests(List<ProductionRequest> ProductionRequests)
        {               
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            foreach(ProductionRequest oneModel in ProductionRequests)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<ProductionRequest>(config.ConnString, config.DbTable, oneModel, oneModel.id);
            }            

            return RedirectToAction("MainProductionRequests");
        }

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult InsertProductionRequest(string customer)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> ProductionRequests = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                        .Where(x => x.Enabled=="1").ToList();            

            ViewBag.ProductionRequestsList = ProductionRequests;
            
            List<ClienteViewModel> allCustomers = dbAccessor.Queryer<ClienteViewModel>(config.CustomerListConnString, config.CustomersDbTable);
            ViewBag.allCustomers = allCustomers;

            List<ArticoloViewModel> allArticles = dbAccessor.Queryer<ArticoloViewModel>(config.ArticlesListConnString, config.ArticlesDbTable)
                                    .Where(c => c.Cliente == customer).ToList();

            bool selArticlesActive = (allArticles.Count == 0)? false : true;

            
            ViewBag.allArticles = allArticles;

            //int customerIndex = allCustomers.IndexOf(allCustomers.Where(C => C.Nome == customer).FirstOrDefault());
            ViewBag.selectedCustomer = customer;
            ViewBag.selArtclesActive = selArticlesActive;

            return View();
        }

        [HttpPost]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult InsertProductionRequest(ProductionRequest newProductionRequest)
        {  
            //analisi dei dati  
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            newProductionRequest = ComputeAvailability(newProductionRequest);

            //------------------------------------------------------------------
            newProductionRequest.CreatedBy = userData.UserName;
            newProductionRequest.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newProductionRequest.Enabled = "1";            

            //----- inserimento con id opportuna in database
            List<ProductionRequest> ProductionRequests = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable);

            long max = 0;

            try {
                max = (from l in ProductionRequests select l.id).Max();            
            } catch (Exception excp) {
                Log2File($"ERRORE: {excp.Message}");
            }
            
            newProductionRequest.id = max + 1;
            int result = dbAccessor.Insertor<ProductionRequest>(config.ConnString, config.DbTable, newProductionRequest);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult ModProductionRequest(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> ProductionRequests = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.ProductionRequestsList = ProductionRequests;
            ProductionRequest oneModel = ProductionRequests.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult ModProductionRequest(ProductionRequest oneModel)
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<ProductionRequest>(config.ConnString, config.DbTable, oneModel, oneModel.id);

            return RedirectToAction("MainProductionRequests");
        }  

        [HttpGet]
        public IActionResult ScheduleProdRequest(long id)
        {
            //leggere TestControllerConfig
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            ProductionRequest request = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                                    .Where(i => i.id == id)
                                                    .FirstOrDefault();

            ViewBag.request = ComputeAvailability(request);
            ViewBag.allAssignments = config.CalendarAssignments.Select(a => a.AssignmentName).ToList();
            //List<string> allAssignments = config.CalendarAssignments.Select(a => a.AssignmentName).ToList();

            return View();

            //{
            //	"assignedTo": "Waterjet",
            //	"eventTitle": "NomeTask",
            //	"eventDescription": "descrizione",
            //	"startDate": "2023-11-29T07:00",
            //	"endDate": "2023-11-29T16:00",
            //	"operationType": "create",
            //	"fileLocation": "test",
            //	"id": "1000"
            //}

        }

        private ProductionRequest ComputeAvailability(ProductionRequest newProductionRequest)        
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            //estrazione codici semilavorati e lastre e verifica quantita necessarie
            ArticoloViewModel thisArticle = dbAccessor.Queryer<ArticoloViewModel>(config.ArticlesListConnString, config.ArticlesDbTable)
                                                        .Where(c => c.Codice == newProductionRequest.Articolo)
                                                        .FirstOrDefault();

            string quantFiniti = dbAccessor.Queryer<ProdFinitiViewModel>(config.ProdFinitiConnString, config.ProdFinitiDbTable)
                                            .Where(c => c.Codice == newProductionRequest.Articolo)
                                            .Select(q => q.Quantita).FirstOrDefault();
            if(quantFiniti==null) quantFiniti ="0"                                            ;
            newProductionRequest.Disponibili = quantFiniti;


            string quantSemilav = dbAccessor.Queryer<SemilavoratoViewModel>(config.SemilavConnString, config.SemilavDbTable)
                                        .Where(c => c.Codice == thisArticle.CodSemilavorato)
                                        .Select(q => q.Quantita).FirstOrDefault();
            if(quantSemilav==null) quantSemilav ="0";
            newProductionRequest.DispSemilav = quantSemilav;
            newProductionRequest.CodSemilavorato = thisArticle.CodSemilavorato;

            string quantLastre = dbAccessor.Queryer<PannelloViewModel>(config.PanelsConnString, config.PanelsDbTable)
                                        .Where(p => p.Codice == thisArticle.CodPannello)
                                        .Select(q => q.Quantita).FirstOrDefault();
            if(quantLastre==null) quantLastre ="0";
            newProductionRequest.DispPannello = quantLastre;
            newProductionRequest.CodPannello = thisArticle.CodPannello;
            
            int semilavPerPan = 1;
            //assegna numero pezzi per pannello da file
            try
            {
                List<PezziPerPannelloViewModel> listaPezzixPannello = LoadPanelsPiecesList(config.PezziPerPannelloList);

                semilavPerPan = listaPezzixPannello.Where(a => a.ArticleCode == newProductionRequest.Articolo)
                                                        .Select(p => p.Pieces).FirstOrDefault(); 
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }
            int richiestaSemilav = 0;       
            int richiestaPan =0;
            
            int richiestaFiniti = Convert.ToInt16(newProductionRequest.Disponibili) - Convert.ToInt16(newProductionRequest.Richiesti);
            if(richiestaFiniti<0)
            {
                richiestaSemilav = Convert.ToInt16(newProductionRequest.DispSemilav) - Math.Abs(richiestaFiniti);
                newProductionRequest.Finitibg = "orange";
            } else newProductionRequest.Finitibg ="none";
            
            if(richiestaSemilav<0)
            {
                richiestaPan = Convert.ToInt16(Math.Truncate((decimal)Math.Abs(richiestaSemilav)/semilavPerPan));
                newProductionRequest.Semilavbg = "orange";
            } else newProductionRequest.Semilavbg ="none";
            
            if((Convert.ToInt16(newProductionRequest.DispPannello)-richiestaPan)<0)
            {
                newProductionRequest.Pannellibg = "orange";
            } else newProductionRequest.Pannellibg = "none";

            return newProductionRequest;       
        }

        private List<PezziPerPannelloViewModel> LoadPanelsPiecesList(string listFileName)
        {
            List<PezziPerPannelloViewModel> result = new List<PezziPerPannelloViewModel>();
            List<string> rawFile = new List<string>();
            try
            {
                rawFile = new List<string>(System.IO.File.ReadAllLines(listFileName));
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }

            try
            {
                foreach(string oneLine in rawFile)
                {
                    string[] parts = oneLine.Split(',');
                    PezziPerPannelloViewModel onePezziPan = new PezziPerPannelloViewModel(){
                        ArticleCode = parts[0],
                        Pieces = Convert.ToInt16(parts[1])
                    };
                    result.Add(onePezziPan);
                }
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }
            return result;
        }


    #endregion

    #region usersDashboard

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult MainUsers()
        {
            //faccio fare tutto qui-non riesco ad elaborare una architettura migliore
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            
            List<UsersDashViewModel> model = new List<UsersDashViewModel>();
            List<DataPoint> dataPoints = new List<DataPoint>();
            
            //prelevo i dati di tutte le tabelle
            List<string> allUsersTables = config.UsersDashTables;
            
            foreach(string oneUsersTable in allUsersTables)
            {
                string ora = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                DateTime adesso = DateTime.ParseExact(ora, "dd/MM/yyyy-HH:mm", null);
                
                switch(oneUsersTable)
                {
                    case "MagazzinoBordi":
                        List<BordoViewModel> allBordi = dbAccessor.Queryer<BordoViewModel>(config.ConnString, oneUsersTable);
                        BordoViewModel lastUpBordo = allBordi.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                            .FirstOrDefault();

                        model.Add(new UsersDashViewModel(){
                                UserName = lastUpBordo.CreatedBy,
                                Table = oneUsersTable,
                                LastUpdated = DateConverter(lastUpBordo.CreatedOn),
                                UpdatedTimes = allBordi.Count(),
                                Distance = GetWorkDuration(DateConverter(lastUpBordo.CreatedOn), DateTime.Now),
                                ForeignId = lastUpBordo.id                        
                        });
                        //-------------------------
                        dataPoints.Add(new DataPoint(
                            "Bordi",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpBordo.CreatedOn), DateTime.Now))),
                            lastUpBordo.CreatedBy));
                        break;

                    case "MagazzinoColle":
                        List<CollaViewModel> allColle = dbAccessor.Queryer<CollaViewModel>(config.ConnString, oneUsersTable);
                        CollaViewModel lastUpColla = allColle.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                        .FirstOrDefault();

                        model.Add(new UsersDashViewModel(){
                                UserName = lastUpColla.CreatedBy,
                                Table = oneUsersTable,
                                LastUpdated = DateConverter(lastUpColla.CreatedOn),
                                UpdatedTimes = allColle.Count(),
                                Distance = GetWorkDuration(DateConverter(lastUpColla.CreatedOn), DateTime.Now),
                                ForeignId = lastUpColla.id                        
                        });
                        //-------------------------
                        dataPoints.Add(new DataPoint(
                            "Colle",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpColla.CreatedOn), DateTime.Now))),
                            lastUpColla.CreatedBy));                                              
                        break;

                    case "MagazzinoPannelli":
                        List<PannelloViewModel> allPanels = dbAccessor.Queryer<PannelloViewModel>(config.ConnString, oneUsersTable);
                        PannelloViewModel lastUpPanel = allPanels.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                                .FirstOrDefault();
                        if(lastUpPanel!=null)
                        {
                            model.Add(new UsersDashViewModel(){
                                    UserName = lastUpPanel.CreatedBy,
                                    Table = oneUsersTable,
                                    LastUpdated = DateConverter(lastUpPanel.CreatedOn),
                                    UpdatedTimes = allPanels.Count(),
                                    Distance = GetWorkDuration(DateConverter(lastUpPanel.CreatedOn), DateTime.Now),
                                    ForeignId = lastUpPanel.id                        
                            });
                            //-------------------------
                            dataPoints.Add(new DataPoint(
                                "Pannelli",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpPanel.CreatedOn), DateTime.Now))),
                                lastUpPanel.CreatedBy));                               
                        }
                        break;

                    case "MagazzinoProdottiFiniti":
                        List<ProdFinitiViewModel> allFinishedProd = dbAccessor.Queryer<ProdFinitiViewModel>(config.ConnString, oneUsersTable);
                        ProdFinitiViewModel lastUpFinish = allFinishedProd.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                                .FirstOrDefault();

                        model.Add(new UsersDashViewModel(){
                                UserName = lastUpFinish.CreatedBy,
                                Table = oneUsersTable,
                                LastUpdated = DateConverter(lastUpFinish.CreatedOn),
                                UpdatedTimes = allFinishedProd.Count(),
                                Distance = GetWorkDuration(DateConverter(lastUpFinish.CreatedOn), DateTime.Now),
                                ForeignId = lastUpFinish.id                        
                        });
                        //-------------------------
                        dataPoints.Add(new DataPoint(
                            "Finiti",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpFinish.CreatedOn), DateTime.Now))),
                            lastUpFinish.CreatedBy));                                       
                        break;

                    case "MagazzinoResti":
                        List<RestoViewModel> allResti = dbAccessor.Queryer<RestoViewModel>(config.ConnString, oneUsersTable);
                        RestoViewModel lastUpResto = allResti.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                            .FirstOrDefault();

                        model.Add(new UsersDashViewModel(){
                                UserName = lastUpResto.CreatedBy,
                                Table = oneUsersTable,
                                LastUpdated = DateConverter(lastUpResto.CreatedOn),
                                UpdatedTimes = allResti.Count(),
                                Distance = GetWorkDuration(DateConverter(lastUpResto.CreatedOn), DateTime.Now),
                                ForeignId = lastUpResto.id                        
                        });
                        //-------------------------
                        dataPoints.Add(new DataPoint(
                            "Resti",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpResto.CreatedOn), DateTime.Now))),
                            lastUpResto.CreatedBy));                                          
                        break;

                    case "MagazzinoSemilavorati":
                        List<SemilavoratoViewModel> allSemilav = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnString, oneUsersTable);
                        SemilavoratoViewModel lastUpSemilav = allSemilav.OrderByDescending(d => DateTime.ParseExact(d.CreatedOn, "dd/MM/yyyy-HH:mm", null))
                                                            .FirstOrDefault();

                        model.Add(new UsersDashViewModel(){
                                UserName = lastUpSemilav.CreatedBy,
                                Table = oneUsersTable,
                                LastUpdated = DateConverter(lastUpSemilav.CreatedOn),
                                UpdatedTimes = allSemilav.Count(),
                                Distance = GetWorkDuration(DateConverter(lastUpSemilav.CreatedOn), DateTime.Now),
                                ForeignId = lastUpSemilav.id                        
                        });
                        //-------------------------
                        dataPoints.Add(new DataPoint(
                            "Semilav",
                            Convert.ToDouble(GetDoubleWorkDuration(
                                                GetWorkDuration(DateConverter(lastUpSemilav.CreatedOn), DateTime.Now))),
                            lastUpSemilav.CreatedBy));                                       
                        break;
                }
            }
 
			ViewBag.DataPoints = JsonConvert.SerializeObject(dataPoints);

            return View(model);
        }

        private TimeSpan GetWorkDuration(DateTime startDate, DateTime endDate)
        {
            TimeSpan workDuration = TimeSpan.Zero;
            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    workDuration = workDuration.Add(new TimeSpan(0, 1, 0));
                }
                currentDate = currentDate.AddMinutes(1);
            }

            return workDuration;
        }

        private double GetDoubleWorkDuration(TimeSpan input)
        {
            double result = Convert.ToDouble(input.Days + input.Hours * 0.0417);
            return result;
        }

    #endregion

    #region UserActivityReport

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult MainUsersActivities()
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------            
            List<UserDbMovementsViewModel> usersMovs = new List<UserDbMovementsViewModel>();
            
            
            List<string>productionTables = config.DbMovementsDbFilter;
            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DbMovementsViewModel> allDbMovements = new List<DbMovementsViewModel>();
            if(userData.UserRoles.Contains("root")||userData.UserRoles.Contains("allUserActivities"))
            {
                allDbMovements = dbAccessor.Queryer<DbMovementsViewModel>(config.DbMovementsConnString, config.DbMovementsTable);
            }
            else
            {
                allDbMovements = dbAccessor.Queryer<DbMovementsViewModel>(config.DbMovementsConnString, config.DbMovementsTable)
                                            .Where(t => productionTables.Contains(t.DbTable))
                                            .ToList();
            }
            

            //creo la lista di ogni user
            List<string> activeUsers = allDbMovements.Select(u => u.User).Distinct().ToList();
            
            foreach(string oneUser in activeUsers)
            {
                List<DbMovementsViewModel> userMovements = allDbMovements.Where(u => u.User == oneUser).ToList();
                DbMovementsViewModel lastUserMov = new DbMovementsViewModel();


                lastUserMov = userMovements.OrderByDescending(d => DateTime.ParseExact(d.ModifiedOn, "dd/MM/yyyy-HH:mm", null))
                                                .FirstOrDefault();


                string modDetails = $"{Path.GetFileNameWithoutExtension(lastUserMov.DbName)}/{lastUserMov.DbTable}/{lastUserMov.DbColumn}/{lastUserMov.OperationType} da {lastUserMov.PreviousVal} a {lastUserMov.NewVal}|{lastUserMov.Code}|{lastUserMov.Description}";
                
                UserDbMovementsViewModel oneUserMovs = new UserDbMovementsViewModel() 
                {
                    User = oneUser,
                    LastMod = DateTime.ParseExact(lastUserMov.ModifiedOn, "dd/MM/yyyy-HH:mm", null),
                    TotalMods = userMovements.Count,
                    LastModDetails = modDetails
                };
                usersMovs.Add(oneUserMovs);
            }            

            return View(usersMovs.OrderBy(n => n.TotalMods).ToList());
        }


    #endregion

        public IActionResult Test()
        {
            return View();
        }

        //----------------- logger

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }

        //-------------- utils

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

        private DateTime DateConverter(string input)
        {
            string format="dd/MM/yyyy-HH:mm";
            DateTime result;
            
            DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

            return result;
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