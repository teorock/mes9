using System;
using System.Collections.Generic;
using System.Security.Claims;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using mes.Models.Services.Application;
using System.Drawing;
using ServiceStack.Text;
using System.Text;
using AutoMapper;
using mes.Models.ControllersConfigModels;
using ServiceStack;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class ProgramsController : Controller
    {
        private readonly ILogger<ProgramsController> _logger;
        private string programsControllerConfigPath = @"c:\core\mes\ControllerConfig\ProgramsController.json";

        ProgramsControllerConfig config = new ProgramsControllerConfig();
        private bool aggiornaBordi = true;
        private bool aggiornaColle = true;
        private bool aggiornaSemilavorati = true;
        //private bool aggiornaMateriali = true;

        public ProgramsController(ILogger<ProgramsController> logger)
        {
            _logger = logger;

            string rawConf = "";

            using (StreamReader sr = new StreamReader(programsControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<ProgramsControllerConfig>(rawConf);                    
        }

        public IActionResult Index()
        {
            return View();
        }
        
        #region bordi

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiLeggi, MagMaterialiScrivi")]
        public IActionResult MagBordi(string filter)
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = new List<BordoViewModel>();

            List<BordoViewModel> tmpBordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                            .Where(x => x.Enabled =="1").ToList();            
            if(filter == null || filter== "")
            {
                bordi = tmpBordi;
            }
            else
            {
                bordi = tmpBordi.Where(x => x.GetType().GetProperties().Any(p => {var value = p.GetValue(x).ToString().ToLower(); return value!=null && value.ToString().Contains(filter.ToLower());})).ToList();
            }

            //aggiornaBordi = true;
            return View("MagBordi", bordi);
        }

        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult AggiornaBordi(List<BordoViewModel> bordi)
        {
            //if(aggiornaBordi)
            //{                
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //estraggo i dati originali
            List<BordoViewModel> actualValues = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                                            .Where(e => e.Enabled =="1")
                                                            .ToList();

            //estraggo solo quelli dove la quantità è camnbiata
                List<BordoViewModel> changed = actualValues
                    .Join(bordi,
                    item1 => item1.id,
                    item2 => item2.id,
                    (item1, item2) => new {Item1 = item1, Item2 = item2})
                    .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                    //.ToList();
                    .Select(pair => pair.Item2)
                    .ToList();

            //scrivo solo quelli
            foreach(BordoViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                oneModel.Enabled ="1";
                int result = dbAccessor.Updater<BordoViewModel>(config.ConnectionString, config.ColleDbTable, oneModel, oneModel.id);
            }  
            //}       
            return RedirectToAction("MagBordi");
        }


        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertBordo()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.BordersList = bordi;
            ViewBag.errorMessage = TempData["errorMessage"];

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertBordo(BordoViewModel newBordo)
        {
            UserData userData = GetUserData();

            newBordo.CreatedBy = userData.UserName;
            newBordo.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newBordo.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> checkBordo = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                                .Where(x => x.Codice ==newBordo.Codice).ToList();
            if(checkBordo.Count > 0)
            {
                //return RedirectToAction("InsertBordo", "Programs", new {errorMessage ="codice già presente}");
                TempData["errorMessage"] = "codice bordo gia\' presente";
                return Redirect(Url.Action("InsertBordo", "Programs"));
            }

            List<BordoViewModel> bordi = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable);      

            long max = (from l in bordi select l.id).Max();

            newBordo.id = max + 1;

            int result = dbAccessor.Insertor<BordoViewModel>(config.ConnectionString, config.BordiDbTable, newBordo);

            return RedirectToAction("MagBordi");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModBordo(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.bordersList = bordi;
            BordoViewModel oneModel = bordi.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModBordo(BordoViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<BordoViewModel>(config.ConnectionString, config.BordiDbTable, oneModel, oneModel.id);

            return RedirectToAction("MagBordi");
        }        


        [HttpPost]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        private bool CheckDoubleRecord(BordoViewModel input)
        {
            return true;
        }

        public IActionResult ExportCsvBordi ()
        {

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                            .Where(x => x.Enabled =="1").ToList();

            var configExp = new MapperConfiguration(cfg => cfg.CreateMap<BordoViewModel, BordoDTO>());
            var mapper = new Mapper(configExp);
            List<BordoDTO> exportBordi = new List<BordoDTO>();

            mapper.Map(bordi, exportBordi);

            //scrivi il csv localmente
            GeneralPurpose genPurpose = new GeneralPurpose();
            //List<string> rawCsv = genPurpose.ObjectList2Csv<SemilavoratoViewModel>(semilavorati);
            string csv = CsvSerializer.SerializeToCsv(exportBordi);

            string outputFile = "exportBordi.csv";
            byte[] fileBytes = Encoding.ASCII.GetBytes(csv);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, outputFile);             
        }

        #endregion

        #region colla

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiLeggi, MagMaterialiScrivi")]
        public IActionResult MagColle()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                            .Where(x => x.Enabled == "1").ToList();
                        
            return View(colle);
        }

        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult AggiornaColle(List<CollaViewModel> colle)
        {
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //estraggo dati originali
            List<CollaViewModel> collePreviousState = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                                                .Where(x => x.Enabled == "1")
                                                                .ToList();                
            //estraggo solo quelli dove la quantità è camnbiata
                List<CollaViewModel> changed = collePreviousState
                    .Join(colle,
                    item1 => item1.id,
                    item2 => item2.id,
                    (item1, item2) => new {Item1 = item1, Item2 = item2})
                    .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                    //.ToList();
                    .Select(pair => pair.Item2)
                    .ToList();
            
            //scrivo solo quelli
            foreach(CollaViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                oneModel.Enabled ="1";
                int result = dbAccessor.Updater<CollaViewModel>(config.ConnectionString, config.ColleDbTable, oneModel, oneModel.id);
            }       
            return RedirectToAction("MagColle");            
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertColla()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                            .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.GluesList = colle;
            ViewBag.errorMessage = TempData["errorMessage"];

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertColla(CollaViewModel newColla)
        {
            UserData userData = GetUserData();

            newColla.CreatedBy = userData.UserName;
            newColla.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newColla.Enabled="1";           

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            //controllo che il codice prodotto non sia già inserito
            List<CollaViewModel> checkColla = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                                .Where(x => x.Codice ==newColla.Codice).ToList();
            if(checkColla.Count > 0)
            {
                //return RedirectToAction("InsertBordo", "Programs", new {errorMessage ="codice già presente}");
                TempData["errorMessage"] = "codice colla gia' presente";
                return Redirect(Url.Action("InsertColla", "Programs"));
            }


            List<CollaViewModel> colle = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable);             

            long max = (from l in colle select l.id).Max();

            newColla.id=max+1;

            int result = dbAccessor.Insertor<CollaViewModel>(config.ConnectionString, config.ColleDbTable, newColla);

            return RedirectToAction("MagColle");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModColla(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                    .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.gluesList = colle;
            CollaViewModel oneModel = colle.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModColla(CollaViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<CollaViewModel>(config.ConnectionString, config.ColleDbTable, oneModel, oneModel.id);

            return RedirectToAction("MagColle");
        }        

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult CancColla(long id)
        {
            aggiornaColle = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            CollaViewModel colla2disable = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable).Where(X => X.id == id).FirstOrDefault();
            colla2disable.Enabled = "0";

            int result = dbAccessor.Updater<CollaViewModel>(config.ConnectionString, config.ColleDbTable, colla2disable, id);

            return RedirectToAction("MagColle");
        }

        public IActionResult ExportCsvColle ()
        {

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                            .Where(x => x.Enabled == "1").ToList();

            var configExp = new MapperConfiguration(cfg => cfg.CreateMap<CollaViewModel, CollaDTO>());
            var mapper = new Mapper(configExp);
            List<CollaDTO> exportColle = new List<CollaDTO>();

            mapper.Map(colle, exportColle);

            //scrivi il csv localmente
            GeneralPurpose genPurpose = new GeneralPurpose();
            //List<string> rawCsv = genPurpose.ObjectList2Csv<SemilavoratoViewModel>(semilavorati);
            string csv = CsvSerializer.SerializeToCsv(exportColle);

            string outputFile = "exportColle.csv";
            byte[] fileBytes = Encoding.ASCII.GetBytes(csv);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, outputFile);             
        }

        #endregion

        #region pannelli

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiLeggi, PannelliScrivi")]
        public IActionResult MagPannelli(string tipoMateriale, string filter)
        {
            if(tipoMateriale=="" || tipoMateriale == null) tipoMateriale="tutti";
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = new List<PannelloViewModel>();
            List<PannelloViewModel> tempPannelli = new List<PannelloViewModel>();
                if(tipoMateriale == "" || tipoMateriale == null || tipoMateriale =="tutti")
                {
                    tempPannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                    .Where(x => x.Enabled =="1").ToList();
                }
                else
                {
                    tempPannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                    .Where(x => x.Enabled =="1")
                                                    .Where(y => y.Tipomateriale== tipoMateriale).ToList();
                }

                if(filter=="" || filter== null)
                {
                    pannelli = tempPannelli;
                }
                else
                {
                    pannelli = tempPannelli.Where(x => x.GetType().GetProperties().Any(p => {var value = p.GetValue(x).ToString().ToLower(); return value!=null && value.ToString().Contains(filter.ToLower());})).ToList();
                }


            ViewBag.NomiMateriali = dbAccessor.Queryer<MaterialiPannelli>(config.ConnectionString, config.MatPannelliDbTable);
            ViewBag.displayedMaterial = tipoMateriale;
            ViewBag.tipoMateriale = tipoMateriale;

            return View(pannelli);

        }

        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult AggiornaPannello(List<PannelloViewModel> pannelli)
        {   
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            
            List<PannelloViewModel> actualValues = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                                .Where(e => e.Enabled =="1")
                                                                .ToList();
            //estraggo solo quelli dove la quantità è camnbiata
                List<PannelloViewModel> changed = actualValues
                    .Join(pannelli,
                    item1 => item1.id,
                    item2 => item2.id,
                    (item1, item2) => new {Item1 = item1, Item2 = item2})
                    .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                    //.ToList();
                    .Select(pair => pair.Item2)
                    .ToList();

            //scrivo solo i modificati
            foreach(PannelloViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable, oneModel, oneModel.id);
            }
  
            return RedirectToAction("MagPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertPannello(string tipoMateriale)
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = new List<PannelloViewModel>();

            if(tipoMateriale == "" || tipoMateriale == null)
            {

                pannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                        .Where(x => x.Enabled =="1").ToList();
            }
            else
            {

                pannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                        .Where(x => x.Enabled =="1")
                                        .Where(y => y.Tipomateriale== tipoMateriale).ToList();
            }         
            
            ViewBag.panelsList = pannelli;
            ViewBag.errorMessage = TempData["errorMessage"];

            ViewBag.Customers = GetCustomers();
            ViewBag.NomiMateriali = dbAccessor.Queryer<MaterialiPannelli>(config.ConnectionString, config.PannelliDbTable);
            ViewBag.defaultDate = DateTime.Now.ToString("yyyy-MM-dd");

            ViewBag.codPannEsistenti = pannelli.Select(c => c.Codice).ToList();
            
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertPannello(PannelloViewModel newPannello)
        {    
            UserData userData = GetUserData();

            newPannello.CreatedBy = userData.UserName;
            newPannello.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newPannello.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //controllo che il codice prodotto non sia già inserito
            List<PannelloViewModel> checkPannello = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                    .Where(x => x.Codice ==newPannello.Codice).ToList();
            if(checkPannello.Count > 0)
            {
                TempData["errorMessage"] = "codice pannello gia' presente";
                return Redirect(Url.Action("InsertPannello", "Programs"));
            }
            

            List<PannelloViewModel> pannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable);

            long max = (from l in pannelli select l.id).Max();

            newPannello.id = max + 1;

            int result = dbAccessor.Insertor<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable, newPannello);

            return RedirectToAction("MagPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModPannello(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.panelsList = pannelli;
            PannelloViewModel oneModel = pannelli.Where(x => x.id == id).FirstOrDefault();

             
            ViewBag.Customers = GetCustomers();
            List<MaterialiPannelli> allMaterials = dbAccessor.Queryer<MaterialiPannelli>(config.ConnectionString, config.MatPannelliDbTable);
            ViewBag.NomiMateriali = allMaterials;

            //---------------------------------
            List<string> materiali = allMaterials.Select(x=> x.Nome).ToList<string>();
            List<string> customers = GetCustomers().Select(x => x.Nome).ToList<string>();

            ViewBag.selectedMaterial = materiali.IndexOf(oneModel.Tipomateriale);
            ViewBag.selectedCustomer = customers.IndexOf(oneModel.Cliente);
            ViewBag.defaultDate = DateTime.Now.ToString("yyyy-MM-dd");

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModPannello(PannelloViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable, oneModel, oneModel.id);

            return RedirectToAction("MagPannelli");
        }        

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi, MagMaterialiLeggi")]
        public IActionResult StampaEtichetta(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 

            PannelloViewModel oneModel = pannelli.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi, MagMaterialiLeggi")]
        public IActionResult StampaEtichetta(long id, int copie)
        {

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 

            PannelloViewModel oneModel = pannelli.Where(x => x.id == id).FirstOrDefault();

            int dimension = 7;
            int dpi = 300;
            int fontSize1= 18;
            int fontSize2 = 24;
            int lineSpacing = 100;
            string printerName = "ZebraZT220";

            string qrCodeString =   $"Cliente: {oneModel.Cliente}\r\nData di ingresso: {oneModel.DataIngresso}\r\n" + 
                                    $"Codice: {oneModel.Codice}\r\nCodice esterno: {oneModel.CodiceEsterno}\r\nMateriale: {oneModel.Tipomateriale}\r\n" + 
                                    $"Colore: {oneModel.Colore}\r\nDimensioni: {oneModel.Lunghezza} x {oneModel.Larghezza}\r\nSpessore: {oneModel.Spessore}\r\n" + 
                                    $"locazione/note: {oneModel.Locazione}";                          
            
            QrCodeGeneratorService qrCodeGen = new QrCodeGeneratorService();
            Bitmap qrCodeImage = qrCodeGen.GenerateQrCode(qrCodeString, dimension);

            PrintLabelService printLabel = new PrintLabelService();
            bool result = printLabel.PrintBitmap(qrCodeImage, dpi, fontSize1, fontSize2, lineSpacing, oneModel.Cliente, oneModel.DataIngresso, printerName, 1150, 590, copie, oneModel.Spessore);

            return RedirectToAction("MagPannelli");
        }
        #endregion

        #region MatPannelli

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiLeggi, PannelliScrivi")]
        public IActionResult MainMatPannelli()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MatPannelloViewModel> MatPannelli = dbAccessor.Queryer<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable)
                                            .Where(x => x.Enabled =="1").ToList();

            return View(MatPannelli);

        }

        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult AggiornaMatPannelli(List<MatPannelloViewModel> MatPannelli)
        {           
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            //estraggo dati originali
            List<MatPannelloViewModel> actualValues = dbAccessor.Queryer<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable)
                                                                .Where(e => e.Enabled =="1")
                                                                .ToList();

            //estraggo solo quelli dove la quantità è camnbiata
                List<MatPannelloViewModel> changed = actualValues
                    .Join(MatPannelli,
                    item1 => item1.id,
                    item2 => item2.id,
                    (item1, item2) => new {Item1 = item1, Item2 = item2})
                    .Where(pair => pair.Item1.Nome != pair.Item2.Nome)
                    //.ToList();
                    .Select(pair => pair.Item2)
                    .ToList();


            foreach(MatPannelloViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable, oneModel, oneModel.id);
            }       
            return RedirectToAction("MainMatPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertMatPannello()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MatPannelloViewModel> MatPannelli = dbAccessor.Queryer<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.MatPannelliList = MatPannelli;
            ViewBag.errorMessage = TempData["errorMessage"];

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertMatPannello(MatPannelloViewModel newMatPannello)
        {
            UserData userData = GetUserData();

            newMatPannello.CreatedBy = userData.UserName;
            newMatPannello.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newMatPannello.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            //controllo che il codice prodotto non sia già inserito
            List<CollaViewModel> checkMateriale = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.MatPannelliDbTable)
                                                .Where(x => x.Codice ==newMatPannello.Nome).ToList();
            if(checkMateriale.Count > 0)
            {
                TempData["errorMessage"] = "nome materiale gia' presente";
                return Redirect(Url.Action("InsertMatPannello", "Programs"));
            }


            List<MatPannelloViewModel> MatPannelli = dbAccessor.Queryer<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable);

            long max = (from l in MatPannelli select l.id).Max();

            newMatPannello.id = max + 1;

            int result = dbAccessor.Insertor<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable, newMatPannello);

            return RedirectToAction("MainMatPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModMatPannello(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MatPannelloViewModel> MatPannelli = dbAccessor.Queryer<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.MatPannelliList = MatPannelli;
            MatPannelloViewModel oneModel = MatPannelli.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModMatPannello(MatPannelloViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<MatPannelloViewModel>(config.ConnectionString, config.MatPannelliDbTable, oneModel, oneModel.id);

            return RedirectToAction("MainMatPannelli");
        }        

        public IActionResult ExportCsvPannelli (string tipoMateriale)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = new List<PannelloViewModel>();

            if(tipoMateriale == "" || tipoMateriale == null || tipoMateriale=="tutti")
            {

                pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                .Where(x => x.Enabled =="1").ToList();
            }
            else
            {

                pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                .Where(x => x.Enabled =="1")
                                                .Where(y => y.Tipomateriale== tipoMateriale).ToList();
            }     

            var configExp = new MapperConfiguration(cfg => cfg.CreateMap<PannelloViewModel, PannelloDTO>());
            var mapper = new Mapper(configExp);
            List<PannelloDTO> exportPannelli = new List<PannelloDTO>();

            mapper.Map(pannelli, exportPannelli);

            //scrivi il csv localmente
            GeneralPurpose genPurpose = new GeneralPurpose();
            //List<string> rawCsv = genPurpose.ObjectList2Csv<SemilavoratoViewModel>(semilavorati);
            string csv = CsvSerializer.SerializeToCsv(exportPannelli);

            string outputFile = "exportPannelli.csv";
            byte[] fileBytes = Encoding.ASCII.GetBytes(csv);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, outputFile);             
        }


        #endregion

        #region Semilavorati

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiLeggi, MagSemilavoratiScrivi")]
        public IActionResult MainSemilavorati(string cliente, string filter)
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> semilavorati = new List<SemilavoratoViewModel>();
            List<SemilavoratoViewModel> tmpSemilavorati = new List<SemilavoratoViewModel>();

            if (cliente =="" || cliente == null || cliente == "tutti")
            {
                tmpSemilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                                .Where(x => x.Enabled =="1").ToList();
            }
            else
                        {
                tmpSemilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                                .Where(x => x.Enabled =="1")
                                                .Where(y => y.Cliente== cliente).ToList();
            }

            //filtro di ricerca
            if(filter == null || filter== "")
            {
                semilavorati = tmpSemilavorati;
            }
            else
            {
                semilavorati = tmpSemilavorati.Where(x => x.GetType().GetProperties().Any(p => {var value = p.GetValue(x).ToString().ToLower(); return value!=null && value.ToString().Contains(filter.ToLower());})).ToList();
            }            

            aggiornaSemilavorati = true;
            ViewBag.Clienti = dbAccessor.Queryer<ClienteViewModel>(config.MesConnectionString, config.ClientiDbTable);
            ViewBag.ClienteSelezionato = cliente;
            
            return View(semilavorati);
        }

        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult AggiornaSemilavorati(List<SemilavoratoViewModel> Semilavorati)
        {
            //if(aggiornaSemilavorati)
            //{                
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //estraggo dati originali
            List<SemilavoratoViewModel> actualValues = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                            .Where(x => x.Enabled == "1").ToList();  

            //estraggo solo quelli dove la quantità è camnbiata
            List<SemilavoratoViewModel> changed = actualValues
                .Join(Semilavorati,
                item1 => item1.id,
                item2 => item2.id,
                (item1, item2) => new {Item1 = item1, Item2 = item2})
                .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                //.ToList();
                .Select(pair => pair.Item2)
                .ToList();

            foreach(SemilavoratoViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable, oneModel, oneModel.id);
            }
            //}       
            return RedirectToAction("MainSemilavorati");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult InsertSemilavorato()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> Semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.SemilavoratiList = Semilavorati;
            ViewBag.Clienti = dbAccessor.Queryer<ClienteViewModel>(config.MesConnectionString, config.ClientiDbTable);
            ViewBag.errorMessage = TempData["errorMessage"];
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult InsertSemilavorato(SemilavoratoViewModel newSemilavorato)
        {
            UserData userData = GetUserData();

            newSemilavorato.CreatedBy = userData.UserName;
            newSemilavorato.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newSemilavorato.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //controllo che il codice prodotto non sia già inserito
            List<SemilavoratoViewModel> checkMateriale = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                                .Where(x => x.Codice ==newSemilavorato.Codice).ToList();
            if(checkMateriale.Count > 0)
            {
                TempData["errorMessage"] = "codice semilavorato gia' presente";
                return Redirect(Url.Action("InsertSemilavorato", "Programs"));
            }

            List<SemilavoratoViewModel> Semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable);

            long max = (from l in Semilavorati select l.id).Max();

            newSemilavorato.id = max + 1;

            int result = dbAccessor.Insertor<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable, newSemilavorato);

            return RedirectToAction("MainSemilavorati");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult ModSemilavorato(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> Semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.SemilavoratiList = Semilavorati;
            SemilavoratoViewModel oneModel = Semilavorati.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult ModSemilavorato(SemilavoratoViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable, oneModel, oneModel.id);

            return RedirectToAction("MainSemilavorati");
        }        

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult CancSemilavorato(long id)
        {
            aggiornaSemilavorati = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();            
            //int result = dbAccessor.Delete(connectionString, "MagazzinoSemilavorati", id);

            SemilavoratoViewModel Semilavorato2disable = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                                                    .Where(x => x.id == id).FirstOrDefault();
            Semilavorato2disable.Enabled = "0";

            int result = dbAccessor.Updater<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable, Semilavorato2disable, id);
            
            Thread.Sleep(1000);
            return RedirectToAction("MainSemilavorati");
        }

        [Authorize(Roles = "root, MagSemilavoratiScrivi, MagSemilavoratiLeggi")]
        public IActionResult ExportCsv(string cliente)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> semilavorati = new List<SemilavoratoViewModel>();
            if (cliente =="" || cliente == null || cliente == "tutti")
            {
                semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                            .Where(x => x.Enabled =="1").ToList();
            }
            else
                        {
                semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                            .Where(x => x.Enabled =="1")
                                            .Where(y => y.Cliente== cliente).ToList();
            }

            var configExp = new MapperConfiguration(cfg => cfg.CreateMap<SemilavoratoViewModel, SemiLavoratoDTO>());
            var mapper = new Mapper(configExp);
            List<SemiLavoratoDTO> exportSemilavorati = new List<SemiLavoratoDTO>();

            mapper.Map(semilavorati,exportSemilavorati);

            //scrivi il csv localmente
            GeneralPurpose genPurpose = new GeneralPurpose();
            //List<string> rawCsv = genPurpose.ObjectList2Csv<SemilavoratoViewModel>(semilavorati);
            string csv = CsvSerializer.SerializeToCsv(exportSemilavorati);

            string outputFile = "exportSemilavorati.csv";
            byte[] fileBytes = Encoding.ASCII.GetBytes(csv);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, outputFile); 

        }


        #endregion

        #region MagResti

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi, PannelliLeggi")]
        public IActionResult MainResti()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<RestoViewModel> Resti = dbAccessor.Queryer<RestoViewModel>(config.ConnectionString, config.RestiDbTable)
                                            .Where(x => x.Enabled =="1").ToList();

            return View(Resti);

        }

        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult AggiornaResti(List<RestoViewModel> Resti)
        {
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //estraggo dati originali
            List<RestoViewModel> actualValues = dbAccessor.Queryer<RestoViewModel>(config.ConnectionString, config.RestiDbTable)
                                            .Where(x => x.Enabled == "1").ToList();                
            //estraggo solo quelli dove la quantità è camnbiata
            List<RestoViewModel> changed = actualValues
                .Join(Resti,
                item1 => item1.id,
                item2 => item2.id,
                (item1, item2) => new {Item1 = item1, Item2 = item2})
                .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                //.ToList();
                .Select(pair => pair.Item2)
                .ToList();

            foreach(RestoViewModel oneModel in changed)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<RestoViewModel>(config.ConnectionString, config.RestiDbTable, oneModel, oneModel.id);
            }
            return RedirectToAction("MainResti");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertResto()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<RestoViewModel> Resti = dbAccessor.Queryer<RestoViewModel>(config.ConnectionString, config.RestiDbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            ViewBag.RestiList = Resti;

            ViewBag.allCustomers = GetCustomers();
            
            List<MaterialiPannelli> allMaterials = dbAccessor.Queryer<MaterialiPannelli>(config.ConnectionString, config.MatPannelliDbTable).ToList();
            ViewBag.allMaterials = allMaterials;            

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult InsertResto(RestoViewModel newResto)
        {
            UserData userData = GetUserData();

            newResto.CreatedBy = userData.UserName;
            newResto.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newResto.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<RestoViewModel> Resti = dbAccessor.Queryer<RestoViewModel>(config.ConnectionString, config.RestiDbTable);

            if(Resti.Count() > 0)
            {
                long max = (from l in Resti select l.id).Max();
                newResto.id = max + 1;
            } else {
                newResto.id = 0;
            }

            int result = dbAccessor.Insertor<RestoViewModel>(config.ConnectionString, config.RestiDbTable, newResto);

            return RedirectToAction("MainResti");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModResto(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<RestoViewModel> Resti = dbAccessor.Queryer<RestoViewModel>(config.ConnectionString, config.RestiDbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.RestiList = Resti;
            RestoViewModel oneModel = Resti.Where(x => x.id == id).FirstOrDefault();

            ViewBag.allCustomers = GetCustomers();

            List<MaterialiPannelli> allMaterials = dbAccessor.Queryer<MaterialiPannelli>(config.ConnectionString, config.MatPannelliDbTable)
                                                            .ToList();
            ViewBag.allMaterials = allMaterials;

            //---------------------------------
            List<string> materiali = allMaterials.Select(x=> x.Nome).ToList<string>();
            List<string> customers = GetCustomers().Select(x => x.Nome).ToList<string>();

            ViewBag.selectedMaterial = materiali.IndexOf(oneModel.Materiale);
            ViewBag.selectedCustomer = customers.IndexOf(oneModel.Cliente);

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModResto(RestoViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<RestoViewModel>(config.ConnectionString, config.RestiDbTable, oneModel, oneModel.id);

            return RedirectToAction("MainResti");
        }    

        #endregion


        #region dbLogger

        private bool ObjectsPropertyValuesComparer(object newObject, object referenceObject)
        {
            var inputProps = referenceObject.GetType().GetProperties();

            foreach(var oneProp in inputProps)
            {
                if(oneProp.Name !="CreatedBy" && oneProp.Name !="CreatedOn")
                {
                    var inputVal = newObject.GetType().GetProperty(oneProp.Name).GetValue(newObject, null).ToString();
                    var actualVal = referenceObject.GetType().GetProperty(oneProp.Name).GetValue(referenceObject, null).ToString();

                    if(inputVal!= actualVal) return false;
                }
            }            

            return true;
        }

        #endregion

        #region dashboardMateriali

        public IActionResult DashboardMateriali()
        {
            
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<PannelloViewModel> pannelli = dbAccessor.Queryer<PannelloViewModel>(config.ConnectionString, config.PannelliDbTable)
                                                            .Where(x => x.Enabled =="1")
                                                            .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();

            List<CollaViewModel> colle = dbAccessor.Queryer<CollaViewModel>(config.ConnectionString, config.ColleDbTable)
                                                    .Where(x => x.Enabled == "1")
                                                    .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();           

            List<BordoViewModel> bordi = dbAccessor.Queryer<BordoViewModel>(config.ConnectionString, config.BordiDbTable)
                                                    .Where(x => x.Enabled =="1")
                                                    .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();                                            

            List<SemilavoratoViewModel>semilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(config.ConnectionString, config.SemilavDbTable)
                                                                .Where(x => x.Enabled =="1")
                                                                .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();

            List<DashboardMaterialiViewModel> alertMateriali = new List<DashboardMaterialiViewModel>();

            //--------------------------------------------
            foreach(PannelloViewModel onePanel in pannelli)
            {
                DashboardMaterialiViewModel oneDash = new DashboardMaterialiViewModel(){
                    Section = "pannelli",
                    Message = $"{onePanel.Nome}, {onePanel.Lunghezza}x{onePanel.Larghezza}x{onePanel.Spessore}, cliente:{onePanel.Cliente}, locazione:{onePanel.Locazione}",
                    BackgroundColor = (Convert.ToInt32(onePanel.Quantita) ==0) ? "alert-danger" : "alert-warning",
                    PreMessage = (Convert.ToInt32(onePanel.Quantita) ==0) ? "ESAURITO " : $"in esaurimento {onePanel.Quantita}/{onePanel.QuantitaMin}"
                };
                alertMateriali.Add(oneDash);
            }            

            foreach(SemilavoratoViewModel oneSemi in semilavorati)
            {
                DashboardMaterialiViewModel oneDash = new DashboardMaterialiViewModel(){
                    Section = "semilavorati",
                    Message = $"{oneSemi.NomeArticolo}, {oneSemi.Lunghezza}x{oneSemi.Larghezza}x{oneSemi.Spessore}, cliente:{oneSemi.Cliente}, tipo bordo:{oneSemi.TipoBordo}",
                    BackgroundColor = (Convert.ToInt32(oneSemi.Quantita) ==0) ? "alert-danger" : "alert-warning",
                    PreMessage = (Convert.ToInt32(oneSemi.Quantita) ==0) ? "ESAURITO " : $"in esaurimento {oneSemi.Quantita}/{oneSemi.QuantitaMin}"
                };
                alertMateriali.Add(oneDash);
            }            
            
            foreach(BordoViewModel oneBordo in bordi)
            {
                DashboardMaterialiViewModel oneDash = new DashboardMaterialiViewModel(){
                    BackgroundColor = (Convert.ToInt32(oneBordo.Quantita) ==0) ? "alert-danger" : "alert-warning",
                    Section = "bordi",
                    Message = $"{oneBordo.Nome}, spessore:{oneBordo.Spessore}, altezza:{oneBordo.Altezza}, fornitore:{oneBordo.Fornitore}",
                    PreMessage = (Convert.ToInt32(oneBordo.Quantita) ==0) ? "ESAURITO " : $"in esaurimento {oneBordo.Quantita}/{oneBordo.QuantitaMin}"
                };
                alertMateriali.Add(oneDash);
            } 

            foreach(CollaViewModel oneColla in colle)
            {
                DashboardMaterialiViewModel oneDash = new DashboardMaterialiViewModel(){
                    Section = "colle",
                    Message = $"{oneColla.Nome}, formato:{oneColla.FormatoColla}",
                    BackgroundColor = (Convert.ToInt32(oneColla.Quantita) ==0) ? "alert-danger" : "alert-warning",
                    PreMessage = (Convert.ToInt32(oneColla.Quantita) ==0) ? "ESAURITO " : $"in esaurimento {oneColla.Quantita}/{oneColla.QuantitaMin}"
                };
                alertMateriali.Add(oneDash);
            }


            int collaLines = ((colle.Count() * 100) <= 400)?400: colle.Count()*100;
            int panLines = ((pannelli.Count() * 100) <= 400)?400: pannelli.Count()*100;
            int semiLines = ((semilavorati.Count() * 100) <= 400)?400: semilavorati.Count()*100;
            int bordiLines = ((bordi.Count() * 100) <= 400)?400: bordi.Count()*100;            

            ViewBag.semiLines = $"{semiLines}px";
            ViewBag.panelLines = $"{panLines}px";
            ViewBag.colleLines = $"{collaLines}px";
            ViewBag.bordiLines = $"{bordiLines}px";            

            List<DashboardMaterialiViewModel> tempOrdered = alertMateriali.OrderByDescending(x => x.BackgroundColor == "alert-danger").ToList();

            return View(tempOrdered);
        }

        private string GetDashMessageColor(string quantita, string quantitaMin)
        {
            int quan = Convert.ToInt32(quantita);
            int quanMin = Convert.ToInt32(quantitaMin);

            return (quan == 0)?"red":"orange";
        }

        #endregion

        #region ProdottiFiniti

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiLeggi, PannelliScrivi")]
        public IActionResult MainFiniti()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProdFinitiViewModel> allItems = dbAccessor.Queryer<ProdFinitiViewModel>(config.ConnectionString, config.MagProdFinitiDbTable).ToList();

            return View(allItems);
        }

        [HttpPost]
        public IActionResult AggiornaProdFinito(List<ProdFinitiViewModel> prodFiniti)
        {
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            //estraggo dati originali
            List<ProdFinitiViewModel> actualValues = dbAccessor.Queryer<ProdFinitiViewModel>(config.ConnectionString, config.ColleDbTable)
                                            .Where(x => x.Enabled == "1").ToList();                
            //estraggo solo quelli dove la quantità è camnbiata
            List<ProdFinitiViewModel> changed = actualValues
                .Join(prodFiniti,
                item1 => item1.id,
                item2 => item2.id,
                (item1, item2) => new {Item1 = item1, Item2 = item2})
                .Where(pair => pair.Item1.Quantita != pair.Item2.Quantita)
                //.ToList();
                .Select(pair => pair.Item2)
                .ToList();

            foreach(ProdFinitiViewModel oneProd in changed)
            {
                oneProd.CreatedBy = userData.UserName;
                oneProd.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<ProdFinitiViewModel>(config.ConnectionString, config.MagProdFinitiDbTable, oneProd, oneProd.id);
            }

            return RedirectToAction("MainFiniti");
        }

        [HttpGet]
        public IActionResult InsertFiniti()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles; 

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ArticoloViewModel> allArticles = dbAccessor.Queryer<ArticoloViewModel>(config.MesConnectionString, config.ArticoliDbTable)
                                                            .ToList();

            ViewBag.allArticles = allArticles;
            //
            List<string> itemsInStock = dbAccessor.Queryer<ProdFinitiViewModel>(config.ConnectionString, config.MagProdFinitiDbTable)
                                                    .Select(c => c.Codice).ToList();

            ViewBag.itemsInStock = itemsInStock;
            return View();
        }

        [HttpPost]
        public IActionResult InsertFiniti(ProdFinitiViewModel input)
        {
            UserData userData = GetUserData();
            input.CreatedBy = userData.UserName;
            input.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            input.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<ProdFinitiViewModel> prodotti = dbAccessor.Queryer<ProdFinitiViewModel>(config.ConnectionString, config.MagProdFinitiDbTable);
            long max = 0;
            if(prodotti.Count!=0)
            {
                max = (from l in prodotti select l.id).Max();
            }
             
            input.id = max + 1;

            int result = dbAccessor.Insertor<ProdFinitiViewModel>(config.ConnectionString, config.MagProdFinitiDbTable, input);


            return RedirectToAction("MainFiniti");
        }

        #endregion

        #region ControllerUtilities

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

        private List<ClienteViewModel> GetCustomers()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<ClienteViewModel> customers = dbAccessor.Queryer<ClienteViewModel>(config.MesConnectionString, config.ClientiDbTable);

            return customers;
        }

        #endregion

    }
}