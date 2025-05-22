using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.InfrastructureModels;
using mes.Models.Services;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuGet.Common;
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

            if(!userData.UserRoles.Contains("PfcCrea") & !userData.UserRoles.Contains("root"))
            {
                allPfc = allPfc.Where(c => c.Completed == "0").ToList();                
            }

            ViewBag.userRoles = userData.UserRoles;

            return View(allPfc);
        }

        [HttpGet]
        [Authorize(Roles ="root, PfcCrea")]
        public IActionResult InsertPfc(string daneaCustomer, string deliveryDate, string EnableFutureSelection)
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

            List<string> customers = clienti.OrderBy(nome => nome.Nome).Select(n => n.Nome).ToList();
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

            bool noCustomer = (daneaCustomer =="" | daneaCustomer is null | daneaCustomer =="null") ? true : false;
            if(EnableFutureSelection is null) EnableFutureSelection ="0";

            if(noCustomer)
            {
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                            .Where(t => t.Taken =="0")
                                            .Where(d => Convert.ToDateTime(d.DataConsegna)<= deliveryLimit.Date)
                                            .Select(n => n.NCommessa).ToList();
            }
            
            if(!noCustomer & EnableFutureSelection == "0")
            {                
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                            .Where(t => t.Taken =="0")
                                            .Where(c => c.Cliente == daneaCustomer)
                                            .Where(d => Convert.ToDateTime(d.DataConsegna)<= deliveryLimit)
                                            .Select(n => n.NCommessa).ToList();
            }

            if(!noCustomer & EnableFutureSelection == "1")
            {                
                allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                            .Where(t => t.Taken =="0")
                                            .Where(c => c.Cliente == daneaCustomer)
                                            //.Where(d => Convert.ToDateTime(d.DataConsegna)<= deliveryLimit)
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
        public async Task<IActionResult> InsertPfc(WorkorderViewModel inputModel)
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

                int workPhases = inputModel.WorkPhases.Count;

                string jsonLavorazioni = JsonConvert.SerializeObject(inputModel.WorkPhases);

                //upload no file
                string allFiles = "";

                if(inputModel.UploadedFiles is not null)
                {
                    await UploadFiles(inputModel.UploadedFiles, $"{inputModel.WorkNumber.Replace('/','_')}");
                    allFiles = String.Join(",", inputModel.UploadedFiles.Select(n => n.FileName));
                }

                PfcModel pcf2insert = new PfcModel() {
                    id=inputModel.id,
                    NumeroCommessa = inputModel.WorkNumber,
                    Cliente = inputModel.Customer,
                    RifEsterno = inputModel.ExternalRef,
                    DataConsegna = Convert.ToDateTime(inputModel.deliveryDate).ToString("dd/MM/yyyy"),
                    LavorazioniJsonString = jsonLavorazioni,
                    Progress = $"0/{workPhases}",
                    PfcFiles = allFiles,
                    Completed = "0",
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

        public async System.Threading.Tasks.Task UploadFiles(ICollection<IFormFile> files, string pfcFolder)
        {        
            if (files != null && files.Count > 0)
            {
                string uploadFolder = config.BaseUploadFolder;

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string betterFileName = file.FileName.Replace(' ','_');
                        //string filePath = Path.Combine(uploadFolder,pfcFolder, file.FileName);
                        string filePath = Path.Combine(uploadFolder,pfcFolder, betterFileName);
                        if(!Directory.Exists(Path.GetDirectoryName(filePath))) Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }
            }
        }

        private void UpdateTakenCsvOrders(string takenOrders, string pfcNumber, string daneaCustomer)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PfcCsvDaneaSourceID> allDaneaOrders = new List<PfcCsvDaneaSourceID>();

            allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSourceID>(config.PfcConnString, config.CsvDaneaTable)
                                        .Where(c => c.Cliente == daneaCustomer).ToList();

            //to do: 
                        //sganciare csv se deselezionati
                        // 1 - quali csv erano associati a questo pfc
                        // 2 - se zero esco
                        // 3 - se di quelli già selezionati ne mancano, li deseleziono
                        //procedo come al solito
            List<string> takenCsvOrders = takenOrders.Split(',').ToList();

            List<PfcCsvDaneaSourceID> relatedDaneaOrders = new List<PfcCsvDaneaSourceID>();
            relatedDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSourceID>(config.PfcConnString, config.CsvDaneaTable)
                                        .Where(t => t.Taken =="1")
                                        .Where(p =>p.PfcNumber == pfcNumber).ToList();
 
            if(relatedDaneaOrders.Count !=0)
            {
                foreach(var oneRelated in relatedDaneaOrders)
                {
                    if(!takenCsvOrders.Contains(oneRelated.NCommessa))
                    {
                        // lo deselezioni
                        oneRelated.Taken = "0";
                        oneRelated.PfcNumber = "---";
                        int res = dbAccessor.Updater<PfcCsvDaneaSourceID>(config.PfcConnString, config.CsvDaneaTable, oneRelated, oneRelated.id);

                    }
                }
            }

            allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSourceID>(config.PfcConnString, config.CsvDaneaTable)
                                        .Where(c => c.Cliente == daneaCustomer).ToList();

            
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
        public IActionResult ModPfc(long inputId, string EnableFutureSelection)
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

            if(EnableFutureSelection is null) EnableFutureSelection = "0";
            //passare tutti i CsvOrders contenuti in questo Pfc
            List<PfcCsvDaneaSource> allDaneaOrders = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                    .Where(n => n.PfcNumber == viewModel.WorkNumber)
                                                    //.Where(d => Convert.ToDateTime(d.DataConsegna)<= viewModel.deliveryDate) //questo era un errore
                                                    .ToList();
            
            //passare tutti i CsvOrders selezionabili per questo cliente
            if(EnableFutureSelection == "0")
            {
            allDaneaOrders.AddRange(dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                    .Where(t => t.Taken =="0")
                                                    .Where(d => Convert.ToDateTime(d.DataConsegna)<= viewModel.deliveryDate)
                                                    .Where(c => c.Cliente == pfc.Cliente)
                                                    .ToList());
            } else{
            allDaneaOrders.AddRange(dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                    .Where(t => t.Taken =="0")                                                    
                                                    //.Where(d => Convert.ToDateTime(d.DataConsegna)<= viewModel.deliveryDate)
                                                    .Where(c => c.Cliente == pfc.Cliente)
                                                    .ToList());                
            }

            ViewBag.allDaneaOrders = allDaneaOrders;
            ViewBag.enableFutureSelection = EnableFutureSelection;


            bool canCreate = true;
            if(userData.UserRoles.Contains("PfcAggiorna") & !userData.UserRoles.Contains("PfcCrea"))
            {
                canCreate = false;
            }
            if(userData.UserRoles.Contains("root")) canCreate = true;
            ViewBag.canCreate = canCreate;

            ////=========== ottengo eventuali file allegati
            List<string> files = pfc.PfcFiles.Split(',').ToList();
            if(files[0]!= " ")
            { 
                ViewBag.pfcFiles = files;
            } else {
                ViewBag.pfcFiles = null ;
            }
            

            return View(viewModel);
        }
        //===================================================================
        [HttpPost]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public async Task<IActionResult> ModPfc(WorkorderViewModel model, string ExistingFiles, IFormFileCollection UploadedFiles)
        {
            if (ModelState.IsValid)
            {
                //Ottengo l'utente connesso
                UserData userData = GetUserData();
                //--------------------------
                string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
                string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
                Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");

                int completed = model.WorkPhases.Where(q => q.QualityCheck == "true").Count();
                int toDo = model.WorkPhases.Count;
                string allDone = "0";
                if(completed == toDo) allDone = "1" ;

                string jsonLavorazioni = JsonConvert.SerializeObject(model.WorkPhases);

                //devo fare il Join dei files esistenti e di quelli nuovi eventuali
                //string filesCsv = String.Join(",", UploadedFiles.Select(f => f.FileName));
                //string allFiles = ExistingFiles + filesCsv;

                //if(ExistingFiles is null) ExistingFiles = "";
                //var allFiles = String.Join(",",ExistingFiles.Split(',')
                //          .Concat(UploadedFiles.Select(f => f.FileName))
                //          .ToList());

        var allFilesCsv = string.Join(",",
            (ExistingFiles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>())
            .Concat(UploadedFiles?.Select(f => f.FileName) ?? Enumerable.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
        );

        //get csv previous state


        PfcModel pcf2update = new PfcModel() {
            id = model.id,
            NumeroCommessa = model.WorkNumber,
            Cliente = model.Customer,
            RifEsterno = model.ExternalRef,
            DataConsegna = Convert.ToDateTime(model.deliveryDate).ToString("dd/MM/yyyy"),
            LavorazioniJsonString = jsonLavorazioni,
            Progress = $"{completed}/{toDo}",
            PfcFiles = allFilesCsv,
            Completed = allDone,
            Enabled = "1",
            CreatedBy = userData.UserName,
            CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm") 
        };

        DatabaseAccessor dbAccessor = new DatabaseAccessor();
        // Update the database record
        var result = dbAccessor.Updater<PfcModel>(config.PfcConnString, config.PfcTable, pcf2update, model.id);

        UpdateTakenCsvOrders(pcf2update.RifEsterno, pcf2update.NumeroCommessa, pcf2update.Cliente);

        //---- Handle file management ----
        string pfcPath = Path.Combine(config.BaseUploadFolder, model.WorkNumber.Replace('/','_'));
        
        // Ensure directory exists
        if (!Directory.Exists(pfcPath))
        {
            Directory.CreateDirectory(pfcPath);
        }
        
        // Parse existing files from the form submission
        List<string> requestedExistingFiles = !string.IsNullOrEmpty(ExistingFiles) 
            ? ExistingFiles.Split(',').Select(f => f.Trim()).ToList() 
            : new List<string>();
            
        // Get files currently in the folder
        List<string> filesInFolder = Directory.Exists(pfcPath) ? 
            Directory.GetFiles(pfcPath).Select(f => Path.GetFileName(f)).ToList() : 
            new List<string>();
            
        // Delete files that are no longer needed
        foreach (string fileInFolder in filesInFolder)
        {
            if (!requestedExistingFiles.Contains(fileInFolder))
            {
                string filepath = Path.Combine(pfcPath, fileInFolder);
                try
                {
                    if (System.IO.File.Exists(filepath))
                    {
                        System.IO.File.Delete(filepath);
                        Log2File($"Deleted file: {filepath}");
                    }
                }
                catch (Exception ex)
                {
                    Log2File($"Error deleting file {filepath}: {ex.Message}");
                }
            }
        }
        
        // Upload new files
        if (UploadedFiles != null && UploadedFiles.Count > 0)
        {
            foreach (var file in UploadedFiles)
            {
                if (file != null && file.Length > 0)
                {
                    // Create the file path
                    string filePath = Path.Combine(pfcPath, file.FileName);
                    
                    try
                    {
                        // Save the file to disk
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        
                        Log2File($"Uploaded file: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Log2File($"Error uploading file {file.FileName}: {ex.Message}");
                    }
                }
            }
        }
        
        // Update the list of files in the database
        List<string> updatedFilesList = Directory.GetFiles(pfcPath)
            .Select(f => Path.GetFileName(f))
            .ToList();
            
        string updatedFilesString = string.Join(",", updatedFilesList);
        
        // Update the database with the new file list if necessary
        if (pcf2update.PfcFiles != updatedFilesString)
        {
            pcf2update.PfcFiles = updatedFilesString;
            dbAccessor.Updater<PfcModel>(config.PfcConnString, config.PfcTable, pcf2update, model.id);
        }


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

        private void KillFile(string fileskill)
        {
            if(System.IO.File.Exists(fileskill)) System.IO.File.Delete(fileskill);
        }

        [HttpGet]
        [Authorize(Roles ="root, PfcAggiorna, PfcCrea")]
        public IActionResult CsvOrderUpload(string message, string showTaken)
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
            

            
            if(showTaken is null | showTaken =="0") {
                showTaken = "0";
                ViewBag.btnMessage = "mostra già assegnati";
                ViewBag.btnValue ="1";
            } else if(showTaken =="1") {
                ViewBag.btnMessage = "mostra da assegnare";
                ViewBag.btnValue ="0";
            }
            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PfcCsvDaneaSource> actuals = dbAccessor.Queryer<PfcCsvDaneaSource>(config.PfcConnString, config.CsvDaneaTable)
                                                        .Where(t => t.Taken == showTaken).ToList();
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

            //check completezza campi del csv che si vuole caricare

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

                    string successMessage = "";
                    if(additionaMessage != "")
                    {
                        successMessage = additionaMessage;
                    } else {
                        successMessage = $"Caricato file: {Path.GetFileName(result.FilePath)} - {additionaMessage}";
                    }
                    
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

    public IActionResult DownloadFile(string nCommessa, string fileName)
    {
        if (string.IsNullOrEmpty(nCommessa) || string.IsNullOrEmpty(fileName))
        {
            return NotFound();
        }

        // Sanitize the filename to prevent directory traversal
        var sanitizedFileName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(sanitizedFileName))
        {
            return NotFound();
        }

        // Construct the full path
        var fullPath = Path.Combine(
            config.BaseUploadFolder,
            $"{nCommessa}_{DateTime.Now.Year}",
            sanitizedFileName
        );

        // Verify the file exists
        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        // Determine content type (you might want to expand this)
        var contentType = "application/octet-stream";
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        if (ext == ".pdf") contentType = "application/pdf";
        else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
        else if (ext == ".png") contentType = "image/png";
        else if (ext == ".doc") contentType = "application/msword";
        else if (ext == ".docx") contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        else if (ext == ".xls") contentType = "application/vnd.ms-excel";
        else if (ext == ".xlsx") contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Return the file
        var fileStream = System.IO.File.OpenRead(fullPath);
        return File(fileStream, contentType, fileName);
    }

public IActionResult ViewFile(string nCommessa, string fileName)
{
    if (string.IsNullOrEmpty(nCommessa) || string.IsNullOrEmpty(fileName))
    {
        return NotFound();
    }

    // Sanitize and construct path
    var sanitizedFileName = Path.GetFileName(fileName);
    var fullPath = Path.Combine(
        config.BaseUploadFolder,
        $"{nCommessa}_{DateTime.Now.Year}",
        sanitizedFileName
    );

    if (!System.IO.File.Exists(fullPath))
    {
        return NotFound();
    }

    var ext = Path.GetExtension(fullPath).ToLowerInvariant();
    
    // For PDFs, serve with inline disposition
    if (ext == ".pdf")
    {
        return PhysicalFile(fullPath, "application/pdf", null); // null filename forces inline display
    }
    // For images
    else if (new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
    {
        return PhysicalFile(fullPath, GetContentType(ext), null);
    }
    
    // For unsupported types, fall back to download
    return DownloadFile(nCommessa, fileName);
}

        public IActionResult LoadCsvToDatabase(string file2load, out string internalMessage)
        {
            //carica file raw in lista oggetti
            PfcServices pfcServices = new PfcServices();
            List<PfcCsvDaneaSource> csvFile = pfcServices.LoadCsvDaneaToList(file2load, config.CsvFilter1, config.CsvFilter2);
            
            // controllo se ci sono oggetti vuoti
            GeneralPurpose genPurpose = new GeneralPurpose();
            bool invalidList = genPurpose.HasEmptyOrNullStringProperty<PfcCsvDaneaSource>(csvFile);
            bool hasInvalidDate = HasInvalidDateFormats<PfcCsvDaneaSource>(csvFile);
            if(invalidList)
            {
                internalMessage = "File .csv contiene campi vuoti - non caricato";
                return RedirectToAction("CsvOrderUpload");
            }

            if(hasInvalidDate)
            {
                internalMessage = "File .csv contiene date in formati non corretti - non caricato";
                return RedirectToAction("CsvOrderUpload");
            }

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

    private bool HasInvalidDateFormats<T>(List<T> dataList)
    {
        if (dataList == null)
        {
            // It's often better to throw an ArgumentNullException here if null lists are unexpected.
            // For now, we'll return false, assuming an empty/null list means no invalid dates.
            Console.WriteLine("The input list is null. No date formats to check.");
            return false;
        }

        // Using .Any() to efficiently stop at the first invalid date found
        bool hasInvalid = dataList.Any(obj =>
        {
            if (obj == null)
            {
                //Console.WriteLine("Found a null object in the list. This could indicate invalid data.");
                return true; // A null object in the list is often considered "invalid" for validation purposes
            }

            DateTime dummyDate; // Used to capture the parsed date if successful

            // Use reflection to get the values of "Data" and "DataConsegna"
            // This makes the method truly generic even though it's looking for specific property *names*.
            // If the properties don't exist on T, GetProperty will return null, and we'll skip the check.
            PropertyInfo dataProp = typeof(T).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo dataConsegnaProp = typeof(T).GetProperty("DataConsegna", BindingFlags.Public | BindingFlags.Instance);

            string dataValue = dataProp?.GetValue(obj) as string;
            string dataConsegnaValue = dataConsegnaProp?.GetValue(obj) as string;

            // Check 'Data' property
            if (!string.IsNullOrEmpty(dataValue) && !DateTime.TryParse(dataValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dummyDate))
            {
                //Console.WriteLine($"Invalid date format detected in 'Data' property: '{dataValue}' for object type '{typeof(T).Name}'.");
                return true; // Invalid date found in 'Data'
            }

            // Check 'DataConsegna' property
            if (!string.IsNullOrEmpty(dataConsegnaValue) && !DateTime.TryParse(dataConsegnaValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out dummyDate))
            {
                //Console.WriteLine($"Invalid date format detected in 'DataConsegna' property: '{dataConsegnaValue}' for object type '{typeof(T).Name}'.");
                return true; // Invalid date found in 'DataConsegna'
            }

            return false; // No invalid date format for this specific object
        });

        return hasInvalid;
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

// Helper method to determine content type based on file extension
private string GetContentType(string path)
{
    var extension = Path.GetExtension(path).ToLowerInvariant();
    
    return extension switch
    {
        ".txt" => "text/plain",
        ".pdf" => "application/pdf",
        ".doc" => "application/vnd.ms-word",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
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