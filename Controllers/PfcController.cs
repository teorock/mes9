using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.InfrastructureModels;
using mes.Models.Services;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class PfcController : Controller
    {
        private readonly ILogger<PfcController> _logger;
        private readonly IFileUploadService _fileUploadService;
        PfcControllerConfig config = new PfcControllerConfig();
        const string mesControllerConfigPath = @"c:\core\mes\ControllerConfig\PfcController.json";   
        const string intranetLog=@"c:\temp\intranet.log";           
        public PfcController(ILogger<PfcController> logger, IFileUploadService fileUploadService)
        {
            _logger = logger;
            _fileUploadService = fileUploadService;

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
            List<PfcModel> allPfc = dbAccessor.Queryer<PfcModel>(config.PfcConnString, config.PfcTable);

            ViewBag.userRoles = userData.UserRoles;

            return View(allPfc);
        }

        [HttpGet]
        [Authorize(Roles ="root, PfcCrea")]
        public IActionResult InsertPfc(string daneaCustomer, string deliveryDate)
        {
            // mettere filtro commesse da Danea per data selezionata

            if(deliveryDate == null) deliveryDate = DateTime.Now.ToString("yyyy-MM-dd");

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
            

            List<PfcModel> allPfc = dbAccessor.Queryer<PfcModel>(config.PfcConnString, config.PfcTable).ToList();
            long lastId = 0;
            if(allPfc.Count !=0)
            {
                lastId = allPfc.Select(i => i.id).Max();
            }
        
            ViewBag.allPfc = allPfc;
            ViewBag.nCommessa = lastId +1;
            ViewBag.nCommessaTitle = $"{lastId + 1}/{DateTime.Now.ToString("yyyy")}";

            //preleva tutti i numeri commessa da PfcDatasource.db, CsvDanea table
            //=================== FILTRI DANEA ====================================
            // 1) nessun filtro
            // 2) solo filtro cliente
            // 3) solo filtro data
            // 4) entrambe 
            List<string> allDaneaOrders = new List<string>();
            DateTime deliveryLimit = Convert.ToDateTime(deliveryDate);

            if(daneaCustomer =="" | daneaCustomer is null | daneaCustomer =="null")
            {
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                            .Where(t => t.Taken =="0")
                                            .Where(d => Convert.ToDateTime(d.DataConsegna)<= deliveryLimit.Date)
                                            .Select(n => n.NCommessa).ToList();
            }
            else
            {                
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                            .Where(t => t.Taken =="0")
                                            .Where(c => c.Cliente == daneaCustomer)
                                            .Where(d => Convert.ToDateTime(d.DataConsegna)<= deliveryLimit)
                                            .Select(n => n.NCommessa).ToList();
            }


            ViewBag.selectedDeliveryDate = deliveryDate;
            ViewBag.selectedCustomer = daneaCustomer;
            ViewBag.selectableOrders = allDaneaOrders;           
            //=====================================================================

            return View();
        }

        [HttpPost]
        [Authorize(Roles ="root, PfcCrea")]
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
                    DataConsegna = Convert.ToDateTime(inputModel.deliveryDate).ToString("dd/MM/yyyy"),
                    LavorazioniJsonString = jsonLavorazioni,
                    Enabled = "1",
                    CreatedBy = userData.UserName,
                    CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };

                //aggiorna il campo Taken delle commesse da Danea
                UpdateTakenCsvOrders(pcf2insert.RifEsterno,pcf2insert.NumeroCommessa, pcf2insert.Cliente);

                DatabaseAccessor dbAccessor = new DatabaseAccessor();

                var result = dbAccessor.Insertor<PfcModel>(config.PfcConnString, config.PfcTable, pcf2insert);
                if(result == 1) return RedirectToAction("Error");
            }
            return RedirectToAction("Index");
        }

        private void UpdateTakenCsvOrders(string takenOrders, string pfcNumber, string daneaCustomer)
        {
                //to do: 
                //update Taken & PfcNumber
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                List<PfcCsvDaneaSourceID> allDaneaOrders = new List<PfcCsvDaneaSourceID>();
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSourceID>(config.PfcConnString, config.CsvDaneaTable)
                                            //.Where(t => t.Taken =="0")
                                            .Where(c => c.Cliente == daneaCustomer).ToList();

                string[] takenCsvOrders = takenOrders.Split(',');
                foreach(string oneCvs in takenCsvOrders)
                {                    
                    PfcCsvDaneaSourceID takenId2update = allDaneaOrders.Where(n => n.NCommessa == oneCvs).FirstOrDefault();
                    takenId2update.Taken = "1";
                    takenId2update.PfcNumber = pfcNumber;

                    int res = dbAccessor.Updater<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable, MiniMapper(takenId2update), takenId2update.id);
                }     
        }

        private PfcCsvDaneaSource MiniMapper(PfcCsvDaneaSourceID inputObj)
        {
            PfcCsvDaneaSource result = new PfcCsvDaneaSource()
            {
                Data = inputObj.Data,
                NCommessa = inputObj.NCommessa,
                Cliente = inputObj.Cliente,
                Stato = inputObj.Stato,
                DataConsegna = inputObj.DataConsegna,
                Commento = inputObj.Commento,
                Taken = inputObj.Taken,
                PfcNumber = inputObj.PfcNumber
            };
            return result;
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
            var pfc = dbAccessor.Queryer<PfcModel>(config.PfcConnString, config.PfcTable)
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
                deliveryDate = DateTime.Parse(pfc.DataConsegna),
                // Convert LavorazioniJsonString to WorkPhases collection
                WorkPhases = JsonConvert.DeserializeObject<List<WorkphaseViewModel>>(pfc.LavorazioniJsonString)
            };

            ViewBag.nCommessa = pfc.id;
            ViewBag.nCommessaTitle = pfc.NumeroCommessa;
            ViewBag.Customers = GetCustomersList(); 
            ViewBag.WorkPhases = GetWorkPhasesList(); 
            ViewBag.Operators = GetOperatorsList(); 

            //passare tutti i CsvOrders per questo cliente
            List<PfcCsvDaneaSource> allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                    .Where(n => n.PfcNumber == viewModel.WorkNumber)
                                                    .Where(d => Convert.ToDateTime(d.DataConsegna)<= viewModel.deliveryDate)
                                                    .ToList();
            
            allDaneaOrders.AddRange(dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                    .Where(t => t.Taken =="0")
                                                    .Where(d => Convert.ToDateTime(d.DataConsegna)<= viewModel.deliveryDate)
                                                    .ToList());

            ViewBag.allDaneaOrders = allDaneaOrders;

            //TO DO:
            //abilitazione parziale per PfcAggiorna
            bool canCreate = true;
            if(userData.UserRoles.Contains("PfcAggiorna") & !userData.UserRoles.Contains("PfcCrea"))
            {
                canCreate = false;
            }
            if(userData.UserRoles.Contains("root")) canCreate = true;
            ViewBag.canCreate = canCreate;


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
                    RifEsterno = model.ExternalRef,
                    DataConsegna = Convert.ToDateTime(model.deliveryDate).ToString("dd/MM/yyyy"),
                    LavorazioniJsonString = jsonLavorazioni,
                    Enabled = "1",
                    CreatedBy = userData.UserName,
                    CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm") 
                };

                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                var result = dbAccessor.Updater<PfcModel>(config.PfcConnString, config.PfcTable, pcf2update, model.id);

                UpdateTakenCsvOrders(pcf2update.RifEsterno,pcf2update.NumeroCommessa, pcf2update.Cliente);

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
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public IActionResult CsvOrderUpload(string message)
        {
            // Configure view model specifically for CSV files
            var viewModel = new FileUploadViewModel
            {
                // File type configuration
                AllowedExtensions = new List<string> { ".csv" },
                AllowedMimeTypes = new List<string> { "text/csv" },
                MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB limit for CSV files
                MaxFileSizeDisplay = "5 MB",
                
                // Form customization
                FormTitle = "CSV Order Upload",
                FormDescription = "Upload a CSV file containing order information",
                ShowFileTypeRestrictions = true,
                
                // Button configuration
                UploadButtonText = "Upload CSV",
                ConfirmBeforeUpload = true,
                UploadConfirmationMessage = "Are you sure you want to upload this CSV file? Existing data might be affected."
            };
            
            UserData userData = GetUserData();
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PfcCsvDaneaSource> actuals = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                        .Where(t => t.Taken =="0").ToList();
            ViewBag.actualCsvDanea = actuals;            
            ViewBag.message = message;

            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public async Task<IActionResult> CsvOrderUpload(FileUploadViewModel model)
        {
            // Get user data for logging
            UserData userData = GetUserData();
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();
            
            // Check if the request is AJAX
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            // Check model state
            if (!ModelState.IsValid)
            {
                string errorMessage = "Please select a valid file.";
                _logger.LogWarning($"Invalid model state in {actionName}: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ViewBag.Message = errorMessage;
                return View(model);
            }
            
            // Validate file is specifically a CSV
            if (model.FileToUpload != null)
            {
                string fileExtension = Path.GetExtension(model.FileToUpload.FileName).ToLowerInvariant();
                if (fileExtension != ".csv")
                {
                    string errorMessage = "Only CSV files are allowed for order uploads.";
                    _logger.LogWarning($"Invalid file type attempted: {fileExtension}");
                    
                    if (isAjaxRequest)
                    {
                        return Json(new { success = false, message = errorMessage });
                    }
                    
                    DatabaseAccessor dbAccessor = new DatabaseAccessor();
                    List<PfcCsvDaneaSource> actuals = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                                .Where(t => t.Taken =="0").ToList();
                    ViewBag.actualCsvDanea = actuals;    

                    ViewBag.Message = errorMessage;
                    return View(model);
                }
            }

            try
            {
                // Use the file upload service with specific settings for CSV files
                var result = await _fileUploadService.UploadFileAsync(model.FileToUpload);

                if (result.Success)
                {
                    // Log the successful upload
                    Log2File($"{userData.UserEmail}-->{controllerName},{actionName} - CSV file uploaded: {result.FilePath}");
                    _logger.LogInformation($"CSV uploaded successfully by {userData.UserEmail}: {Path.GetFileName(result.FilePath)}");
                    
                    string additionaMessage ="";
                    LoadCsvToDatabase(result.FilePath, out additionaMessage);

                    string successMessage = $"Caricato file: {Path.GetFileName(result.FilePath)} - {additionaMessage}";
                    
                    if (isAjaxRequest)
                    {
                        return Json(new { 
                            success = true, 
                            message = successMessage,
                            filePath = result.FilePath,
                            fileName = Path.GetFileName(result.FilePath)
                        });
                    }
                    
                    // Provide feedback to the user
                    ViewBag.Message = successMessage;
                    //passo io il nome del file
                    ViewBag.filename = result.FilePath;

                    DatabaseAccessor dbAccessor = new DatabaseAccessor();
                    List<PfcCsvDaneaSource> actuals = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                                .Where(t => t.Taken =="0").ToList();
                    ViewBag.actualCsvDanea = actuals;                        

                    return View(new FileUploadViewModel
                    {
                        FormTitle = "CSV Order Upload",
                        FormDescription = "Upload a CSV file containing order information",
                        AllowedExtensions = new List<string> { ".csv" }
                    });
                }
                else
                {
                    // Log the error
                    Log2File($"{userData.UserEmail}-->{controllerName},{actionName} - Upload failed: {result.ErrorMessage}");
                    _logger.LogError($"CSV upload failed for user {userData.UserEmail}: {result.ErrorMessage}");
                    
                    if (isAjaxRequest)
                    {
                        return Json(new { success = false, message = result.ErrorMessage });
                    }
                    
                    // Provide error feedback to the user
                    ViewBag.Message = result.ErrorMessage;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                // Handle any unexpected exceptions
                string errorMessage = $"An error occurred during file upload: {ex.Message}";
                Log2File($"{userData.UserEmail}-->{controllerName},{actionName} - Exception: {ex.Message}");
                _logger.LogError(ex, $"Exception during CSV upload by {userData.UserEmail}");
                
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ViewBag.Message = errorMessage;
                return View(model);
            }
        }


        public IActionResult LoadCsvToDatabase(string file2load, out string internalMessage)
        {
            //carica file raw in lista oggetti
            PfcServices pfcServices = new PfcServices();
            List<PfcCsvDaneaSource> csvFile = pfcServices.LoadCsvDaneaToList(file2load, config.CsvFilter1, config.CsvFilter2);
            
            //mette la lista su database
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //verifica se i record sono già presenti
            //prelevo tutti
            List<PfcCsvDaneaSource> actuals = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable);

            //ottieni differenza tra le liste
            List<string> actualCommesse = actuals.Select(n => n.NCommessa).ToList();
            List<PfcCsvDaneaSource> newOnes = csvFile.Where(n => !actualCommesse.Contains(n.NCommessa)).ToList();
            
            if(newOnes.Count==0)
            {
                internalMessage = "nessun nuovo record caricato";
            }
            else
            {
                foreach(var oneObj in newOnes)
                {
                    oneObj.PfcNumber = "---";
                    oneObj.Taken="0";
                    int res = dbAccessor.Insertor<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable, oneObj);
                }                
                internalMessage = $"caricati {newOnes.Count} nuovi record";
            }


            return RedirectToAction("CsvOrderUpload");
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