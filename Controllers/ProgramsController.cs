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

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class ProgramsController : Controller
    {
        private readonly ILogger<ProgramsController> _logger;
        private readonly string connectionString ="Data Source=../mesData/programs.db";
        private readonly string mesConnectionString ="Data Source=../mesData/datasource.db";
        private bool aggiornaBordi = true;
        private bool aggiornaColle = true;
        private bool aggiornaSemilavorati = true;
        //private bool aggiornaMateriali = true;

        public ProgramsController(ILogger<ProgramsController> logger)
        {
            _logger = logger;          
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

            List<BordoViewModel> tmpBordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
                                            .Where(x => x.Enabled =="1").ToList();            
            if(filter == null || filter== "")
            {
                bordi = tmpBordi;
            }
            else
            {
                bordi = tmpBordi.Where(x => x.GetType().GetProperties().Any(p => {var value = p.GetValue(x).ToString().ToLower(); return value!=null && value.ToString().Contains(filter.ToLower());})).ToList();
            }

            aggiornaBordi = true;
            return View("MagBordi", bordi);
        }

        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult AggiornaBordi(List<BordoViewModel> bordi)
        {
            if(aggiornaBordi)
            {                
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();

                List<BordoViewModel> actualValues = dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi");

                for(int x=0; x<bordi.Count; x++)
                {                    
                    if(!ObjectsPropertyValuesComparer(actualValues[x], bordi[x]))
                    {
                        BordoViewModel oneModel = bordi[x];

                        oneModel.CreatedBy = userData.UserName;
                        oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                        int result = dbAccessor.Updater<BordoViewModel>(connectionString, "MagazzinoBordi", oneModel, oneModel.id);
                    }
                }
            }       
            return RedirectToAction("MagBordi");
        }


        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertBordo()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
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
            List<BordoViewModel> checkBordo = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
                                                .Where(x => x.Codice ==newBordo.Codice).ToList();
            if(checkBordo.Count > 0)
            {
                //return RedirectToAction("InsertBordo", "Programs", new {errorMessage ="codice già presente}");
                TempData["errorMessage"] = "codice bordo gia\' presente";
                return Redirect(Url.Action("InsertBordo", "Programs"));
            }

            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi");      

            long max = (from l in bordi select l.id).Max();

            newBordo.id = max + 1;

            int result = dbAccessor.Insertor<BordoViewModel>(connectionString, "MagazzinoBordi", newBordo);

            return RedirectToAction("MagBordi");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModBordo(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
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

            int result = dbAccessor.Updater<BordoViewModel>(connectionString,"MagazzinoBordi", oneModel, oneModel.id);

            return RedirectToAction("MagBordi");
        }        

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult CancBordo(long id)
        {
            aggiornaBordi = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();            
            //int result = dbAccessor.Delete(connectionString, "MagazzinoBordi", id);

            BordoViewModel bordo2disable = dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi").Where(x => x.id == id).FirstOrDefault();
            bordo2disable.Enabled = "0";

            int result = dbAccessor.Updater<BordoViewModel>(connectionString,"MagazzinoBordi", bordo2disable, id);
            
            Thread.Sleep(1000);
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
            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
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
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
                                            .Where(x => x.Enabled == "1").ToList();
                        
            aggiornaColle = true;
            return View(colle);
        }

        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult AggiornaColle(List<CollaViewModel> colle)
        {
            if(aggiornaColle)
            {
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(CollaViewModel oneModel in colle)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    oneModel.Enabled ="1";
                    int result = dbAccessor.Updater<CollaViewModel>(connectionString, "MagazzinoColle", oneModel, oneModel.id);
                }
            }            
            return RedirectToAction("MagColle");            
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult InsertColla()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
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
            List<CollaViewModel> checkColla = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
                                                .Where(x => x.Codice ==newColla.Codice).ToList();
            if(checkColla.Count > 0)
            {
                //return RedirectToAction("InsertBordo", "Programs", new {errorMessage ="codice già presente}");
                TempData["errorMessage"] = "codice colla gia' presente";
                return Redirect(Url.Action("InsertColla", "Programs"));
            }


            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle");             

            long max = (from l in colle select l.id).Max();

            newColla.id=max+1;

            int result = dbAccessor.Insertor<CollaViewModel>(connectionString, "MagazzinoColle", newColla);

            return RedirectToAction("MagColle");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult ModColla(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
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

            int result = dbAccessor.Updater<CollaViewModel>(connectionString,"MagazzinoColle", oneModel, oneModel.id);

            return RedirectToAction("MagColle");
        }        

        [HttpGet]
        [Authorize(Roles = "root, MagMaterialiScrivi")]
        public IActionResult CancColla(long id)
        {
            aggiornaColle = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            CollaViewModel colla2disable = dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle").Where(X => X.id == id).FirstOrDefault();
            colla2disable.Enabled = "0";

            int result = dbAccessor.Updater<CollaViewModel>(connectionString,"MagazzinoColle", colla2disable, id);

            return RedirectToAction("MagColle");
        }

        public IActionResult ExportCsvColle ()
        {

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
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
                    tempPannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                                    .Where(x => x.Enabled =="1").ToList();
                }
                else
                {
                    tempPannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
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


            ViewBag.NomiMateriali = (List<MaterialiPannelli>)dbAccessor.Queryer<MaterialiPannelli>(connectionString, "MaterialiPannelli");
            ViewBag.displayedMaterial = tipoMateriale;
            ViewBag.tipoMateriale = tipoMateriale;

            return View(pannelli);

        }

        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult AggiornaPannello(List<PannelloViewModel> pannelli)
        {   
            //comparo gli oggetti e aggiorno solo quelli modificati
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            foreach(PannelloViewModel oneModel in pannelli)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<PannelloViewModel>(connectionString, "MagazzinoPannelli", oneModel, oneModel.id);
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

                pannelli = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                        .Where(x => x.Enabled =="1").ToList();
            }
            else
            {

                pannelli = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                        .Where(x => x.Enabled =="1")
                                        .Where(y => y.Tipomateriale== tipoMateriale).ToList();
            }         
            
            ViewBag.panelsList = pannelli;
            ViewBag.errorMessage = TempData["errorMessage"];

            ViewBag.Customers = GetCustomers();
            ViewBag.NomiMateriali = dbAccessor.Queryer<MaterialiPannelli>(connectionString, "MaterialiPannelli");
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
            List<PannelloViewModel> checkPannello = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                                    .Where(x => x.Codice ==newPannello.Codice).ToList();
            if(checkPannello.Count > 0)
            {
                //return RedirectToAction("InsertBordo", "Programs", new {errorMessage ="codice già presente}");
                TempData["errorMessage"] = "codice pannello gia' presente";
                return Redirect(Url.Action("InsertPannello", "Programs"));
            }
            

            List<PannelloViewModel> pannelli = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli");

            long max = (from l in pannelli select l.id).Max();

            newPannello.id = max + 1;

            //verifica se il record è già presente
            //bool isInputPresent = dbAccessor.CheckDoubleRecord(pannelli, newPannello);

            int result = dbAccessor.Insertor<PannelloViewModel>(connectionString, "MagazzinoPannelli", newPannello);

            return RedirectToAction("MagPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModPannello(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.panelsList = pannelli;
            PannelloViewModel oneModel = pannelli.Where(x => x.id == id).FirstOrDefault();

             
            ViewBag.Customers = GetCustomers();
            List<MaterialiPannelli> allMaterials = dbAccessor.Queryer<MaterialiPannelli>(connectionString, "MaterialiPannelli");
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

            int result = dbAccessor.Updater<PannelloViewModel>(connectionString, "MagazzinoPannelli", oneModel, oneModel.id);

            return RedirectToAction("MagPannelli");
        }        

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult CancPannello(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();            

            PannelloViewModel bordo2disable = dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli").Where(x => x.id == id).FirstOrDefault();
            bordo2disable.Enabled = "0";

            int result = dbAccessor.Updater<PannelloViewModel>(connectionString, "MagazzinoPannelli", bordo2disable, id);
            
            Thread.Sleep(1000);
            return RedirectToAction("MagPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi, MagMaterialiLeggi")]
        public IActionResult StampaEtichetta(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                        .Where(x => x.Enabled=="1").ToList(); 

            PannelloViewModel oneModel = pannelli.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PannelliScrivi, MagMaterialiLeggi")]
        public IActionResult StampaEtichetta(long id, int copie)
        {

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
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
            List<MatPannelloViewModel> MatPannelli = (List<MatPannelloViewModel>)dbAccessor.Queryer<MatPannelloViewModel>(connectionString, "MaterialiPannelli")
                                            .Where(x => x.Enabled =="1").ToList();

            return View(MatPannelli);

        }

        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult AggiornaMatPannelli(List<MatPannelloViewModel> MatPannelli)
        {           
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            foreach(MatPannelloViewModel oneModel in MatPannelli)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<MatPannelloViewModel>(connectionString, "MaterialiPannelli", oneModel, oneModel.id);
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
            List<MatPannelloViewModel> MatPannelli = (List<MatPannelloViewModel>)dbAccessor.Queryer<MatPannelloViewModel>(connectionString, "MaterialiPannelli")
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
            List<CollaViewModel> checkMateriale = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MaterialiPannelli")
                                                .Where(x => x.Codice ==newMatPannello.Nome).ToList();
            if(checkMateriale.Count > 0)
            {
                TempData["errorMessage"] = "nome materiale gia' presente";
                return Redirect(Url.Action("InsertMatPannello", "Programs"));
            }


            List<MatPannelloViewModel> MatPannelli = (List<MatPannelloViewModel>)dbAccessor.Queryer<MatPannelloViewModel>(connectionString, "MaterialiPannelli");

            long max = (from l in MatPannelli select l.id).Max();

            newMatPannello.id = max + 1;

            int result = dbAccessor.Insertor<MatPannelloViewModel>(connectionString, "MaterialiPannelli", newMatPannello);

            return RedirectToAction("MainMatPannelli");
        }

        [HttpGet]
        [Authorize(Roles = "root, PannelliScrivi")]
        public IActionResult ModMatPannello(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<MatPannelloViewModel> MatPannelli = (List<MatPannelloViewModel>)dbAccessor.Queryer<MatPannelloViewModel>(connectionString, "MaterialiPannelli")
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

            int result = dbAccessor.Updater<MatPannelloViewModel>(connectionString,"MaterialiPannelli", oneModel, oneModel.id);

            return RedirectToAction("MainMatPannelli");
        }        

        //[HttpGet]
        //[Authorize(Roles = "root, PannelliScrivi")]
        //public IActionResult CancMatPannello(long id)
        //{
        //    DatabaseAccessor dbAccessor = new DatabaseAccessor();            
//
        //    MatPannelloViewModel bordo2disable = dbAccessor.Queryer<MatPannelloViewModel>(connectionString, "MaterialiPannelli").Where(x => x.id == id).FirstOrDefault();
        //    bordo2disable.Enabled = "0";
//
        //    int result = dbAccessor.Updater<MatPannelloViewModel>(connectionString,"MaterialiPannelli", bordo2disable, id);
        //    
        //    Thread.Sleep(1000);
        //    return RedirectToAction("MainMatPannelli");
        //}

        public IActionResult ExportCsvPannelli (string tipoMateriale)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PannelloViewModel> pannelli = new List<PannelloViewModel>();

            if(tipoMateriale == "" || tipoMateriale == null || tipoMateriale=="tutti")
            {

                pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                                .Where(x => x.Enabled =="1").ToList();
            }
            else
            {

                pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
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
                tmpSemilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
                                                .Where(x => x.Enabled =="1").ToList();
            }
            else
                        {
                tmpSemilavorati = dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
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
            ViewBag.Clienti = dbAccessor.Queryer<ClienteViewModel>(mesConnectionString, "Clienti");
            ViewBag.ClienteSelezionato = cliente;
            
            return View(semilavorati);
        }

        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult AggiornaSemilavorati(List<SemilavoratoViewModel> Semilavorati)
        {
            if(aggiornaSemilavorati)
            {                
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(SemilavoratoViewModel oneModel in Semilavorati)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    int result = dbAccessor.Updater<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati", oneModel, oneModel.id);
                }
            }       
            return RedirectToAction("MainSemilavorati");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult InsertSemilavorato()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> Semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.SemilavoratiList = Semilavorati;
            ViewBag.Clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(mesConnectionString, "Clienti");
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
            List<SemilavoratoViewModel> checkMateriale = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
                                                .Where(x => x.Codice ==newSemilavorato.Codice).ToList();
            if(checkMateriale.Count > 0)
            {
                TempData["errorMessage"] = "codice semilavorato gia' presente";
                return Redirect(Url.Action("InsertSemilavorato", "Programs"));
            }

            List<SemilavoratoViewModel> Semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati");

            long max = (from l in Semilavorati select l.id).Max();

            newSemilavorato.id = max + 1;

            int result = dbAccessor.Insertor<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati", newSemilavorato);

            return RedirectToAction("MainSemilavorati");
        }

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult ModSemilavorato(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<SemilavoratoViewModel> Semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
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

            int result = dbAccessor.Updater<SemilavoratoViewModel>(connectionString,"MagazzinoSemilavorati", oneModel, oneModel.id);

            return RedirectToAction("MainSemilavorati");
        }        

        [HttpGet]
        [Authorize(Roles = "root, MagSemilavoratiScrivi")]
        public IActionResult CancSemilavorato(long id)
        {
            aggiornaSemilavorati = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();            
            //int result = dbAccessor.Delete(connectionString, "MagazzinoSemilavorati", id);

            SemilavoratoViewModel Semilavorato2disable = dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati").Where(x => x.id == id).FirstOrDefault();
            Semilavorato2disable.Enabled = "0";

            int result = dbAccessor.Updater<SemilavoratoViewModel>(connectionString,"MagazzinoSemilavorati", Semilavorato2disable, id);
            
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
                semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
                                                .Where(x => x.Enabled =="1").ToList();
            }
            else
                        {
                semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
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

        #region ProductionCalendar

        [HttpGet]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult ProductionCalendar()
        {
            //calcola il nmero della settimanar;
            int weekNumber = GetWeekNumber();          
            ViewBag.WeekNumber = weekNumber;
            //--------------------

            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendar> calendar = (List<ProductionCalendar>)dbAccessor.Queryer<ProductionCalendar>(connectionString, "CalendarioGantt")
                                            .Where(x => x.Enabled =="1")
                                            .Where(y=> y.WeekNumber == weekNumber.ToString()).ToList();

            ViewBag.ProductionList = calendar;
            ViewBag.AssignedTo = GetGanttAssignementList();

            return View();
        }
        [HttpPost]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult ProductionCalendar(ProductionCalendar inputModel)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendar> calendar = (List<ProductionCalendar>)dbAccessor.Queryer<ProductionCalendar>(connectionString, "CalendarioGantt")
                                            .Where(x => x.Enabled =="1").ToList();            
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult InsertGanttTask(ProductionCalendar inputModel)
        {
            UserData userData = GetUserData();

            inputModel.CreatedBy = userData.UserName;
            inputModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            inputModel.Enabled = "1";
            inputModel.CompletionPerc ="0";
            inputModel.StartDate = ChangeDateFormat(inputModel.StartDate);
            inputModel.EndDate = ChangeDateFormat(inputModel.EndDate);

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<ProductionCalendar> tasks = (List<ProductionCalendar>)dbAccessor.Queryer<ProductionCalendar>(connectionString, "CalendarioGantt");      

            long max = (from l in tasks select l.id).Max();

            inputModel.id = max + 1;

            int result = dbAccessor.Insertor<ProductionCalendar>(connectionString, "CalendarioGantt", inputModel);

            return RedirectToAction("ProductionCalendar");
        }

        [HttpGet]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult ModProdCalendar(long id)
        {
            int weekNumber = GetWeekNumber(); 

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendar> calendar = (List<ProductionCalendar>)dbAccessor.Queryer<ProductionCalendar>(connectionString, "CalendarioGantt")
                                        .Where(x => x.Enabled=="1")
                                        .Where(y=> y.WeekNumber == weekNumber.ToString()).ToList();
                                        
            ViewBag.ProductionList = calendar;
            
            ProductionCalendar oneModel = calendar.Where(x => x.id == id).FirstOrDefault();

            List<string> assignments = dbAccessor.Queryer<ProductionCalendarAssignment>(connectionString, "AssegnaGantt").Select(x => x.AssignedTo).ToList();

            ViewBag.AssignedTo = assignments;
            ViewBag.AssignedToIndex = assignments.IndexOf(oneModel.AssignedTo);
            ViewBag.StartDate = ReverseDate(oneModel.StartDate); 
            ViewBag.EndDate = ReverseDate(oneModel.EndDate);

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, CreaGantt")]
        private string ReverseDate(string inputDate)
        {
            DateTime dateTime = Convert.ToDateTime(inputDate);

            return $"{dateTime.Year}-{dateTime.Month}-{dateTime.Day}";
        }

        [HttpPost]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult ModProdCalendar(ProductionCalendar oneModel)
        {
           ////write
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";
            oneModel.CompletionPerc = "0";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<ProductionCalendar>(connectionString,"CalendarioGantt", oneModel, oneModel.id);

            return RedirectToAction("ProductionCalendar");
        }

        [HttpGet]
        [Authorize(Roles = "root, CreaGantt")]
        public IActionResult DeliverGanttWeek(int week)
        {
            //che settimana è
            //calcola il nmero della settimana          
            int actualWeek = GetWeekNumber();
            //--------------------            
            //estrai tutti i dati di quella settimana
            DatabaseAccessor dbAccessor = new DatabaseAccessor();  
            List<ProductionCalendar> allTasks = (List<ProductionCalendar>)dbAccessor.Queryer<ProductionCalendar>(connectionString, "CalendarioGantt")
                                                .Where(y => y.Enabled =="1")
                                                .Where(x => x.WeekNumber == actualWeek.ToString()).ToList();   
            //compili il file
            //string json2write = GanttCoreCompiler(allTasks);       

            string destFile = @"c:\temp\master.json";
            
            GeneralPurpose genPurpose = new GeneralPurpose();
            genPurpose.BackupCalendarFile(destFile);

            WriteString2Disk(GanttCoreCompiler(allTasks), destFile);

            //lo copi in zona pubblica

            string server = "192.168.2.128";
            string user = "Calendario";
            string pwd = "C4l3nd4r10";

            FtpService ftpService = new FtpService(server, user, pwd);

            string res = ftpService.FtpUploadFile(destFile, Path.GetFileName(destFile));

            return View();
        }


        private int GetWeekNumber()
        {
            DateTime dt = DateTime.Now;
            Calendar cal = new CultureInfo("it-IT").Calendar;            
            
            return cal.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Monday) - 1;
        }


        public string GanttCoreCompiler(List<ProductionCalendar> inputList)
        {       
            List<Task> taskList = new List<Task>();
            List<string> emptyAssigns = new List<string>();
            
            for(int x=0; x<inputList.Count; x++)
            {
                ProductionCalendar oneTask = inputList[x];
                Task singleTask = new Task(){
                    id=IdGenerator(x),
                    name = oneTask.TaskName,
                    progress = Convert.ToInt32(oneTask.CompletionPerc),
                    progressByWorklog = false,
                    description = oneTask.Description,
                    code = oneTask.AssignedTo,
                    level = 0,
                    status = AssignColor(oneTask.AssignedTo),
                    depends = "",
                    start = DateConverter(oneTask.StartDate),
                    duration =TaskDuration(oneTask.StartDate, oneTask.EndDate),
                    end = DateConverter(oneTask.EndDate),
                    startIsMilestone = false,
                    endIsMilestone = false,
                    collapsed = false,
                    canWrite = false,
                    canAdd=false,
                    canDelete=false,
                    canAddIssue=false,
                    assigs=emptyAssigns             
                };

                taskList.Add(singleTask);
            }
                                    
            string jsonString = JsonConvert.SerializeObject(taskList);
            string head = "{\"tasks\":";
            //string tail = ",\"selectedRow\":0,\"deletedTaskIds\":[],\"resources\":[{\"id\":\"tmp_1\",\"name\":\"RvB PLAST\"},{\"id\":\"tmp_2\",\"name\":\"RvB 1836\"},{\"id\":\"tmp_3\",\"name\":\"RvB 1536\"},{\"id\":\"tmp_4\",\"name\":\"Akron\"},{\"id\":\"tmp_5\",\"name\":\"EasyJet 512\"},{\"id\":\"tmp_6\",\"name\":\"EasyJet 480\"},{\"id\":\"tmp_7\",\"name\":\"SCM Record 100\"},{\"id\":\"tmp_8\",\"name\":\"Waterjet\"},{\"id\":\"tmp_9\",\"name\":\"Akron\"},{\"id\":\"tmp_10\",\"name\":\"Incollaggio\"},{\"id\":\"tmp_11\",\"name\":\"Montaggio\"}],\"roles\":[{\"id\":\"tmp_1\",\"name\":\"Project Manager\"},{\"id\":\"tmp_2\",\"name\":\"Worker\"},{\"id\":\"tmp_3\",\"name\":\"Stakeholder\"},{\"id\":\"tmp_4\",\"name\":\"Customer\"}],\"canAdd\":false,\"canWrite\":false,\"canWriteOnParent\":false,\"zoom\":\"1w\"}";
            string tail = ",\"selectedRow\":0,\"deletedTaskIds\":[],\"resources\":[{\"id\":\"tmp_1\",\"name\":\"RvB PLAST\"},{\"id\":\"tmp_2\",\"name\":\"RvB 1836\"},{\"id\":\"tmp_3\",\"name\":\"RvB 1536\"},{\"id\":\"tmp_4\",\"name\":\"Akron\"},{\"id\":\"tmp_5\",\"name\":\"EasyJet 512\"},{\"id\":\"tmp_6\",\"name\":\"EasyJet 480\"},{\"id\":\"tmp_7\",\"name\":\"SCM Record 100\"},{\"id\":\"tmp_8\",\"name\":\"Waterjet\"},{\"id\":\"tmp_9\",\"name\":\"Akron\"},{\"id\":\"tmp_10\",\"name\":\"Incollaggio\"},{\"id\":\"tmp_11\",\"name\":\"Montaggio\"},{\"id\":\"tmp_12\",\"name\":\"Stefani X\"},{\"id\":\"tmp_13\",\"name\":\"Ufficio tecnico\"}],\"roles\":[{\"id\":\"tmp_1\",\"name\":\"Project Manager\"},{\"id\":\"tmp_2\",\"name\":\"Worker\"},{\"id\":\"tmp_3\",\"name\":\"Stakeholder\"},{\"id\":\"tmp_4\",\"name\":\"Customer\"}],\"canAdd\":false,\"canWrite\":false,\"canWriteOnParent\":false,\"zoom\":\"1w\"}";
            
            return head + jsonString + tail;
        }


        private double TaskDuration(string startDate, string endDate)
        {
            DateTime selectedStartDate = Convert.ToDateTime(startDate);
            DateTime selectedEndDate = Convert.ToDateTime(endDate);

            double dayLong = ((selectedEndDate - selectedStartDate).TotalDays + 1);

            return dayLong;           
        }

        private double DateConverter(string inputDate)        
        {
            DateTime baseDate = new DateTime(1970, 1, 1);
            DateTime selectedDate = Convert.ToDateTime(inputDate);

            TimeSpan elapsedDate = selectedDate - baseDate;

            double millisecDate = elapsedDate.TotalMilliseconds;

            return millisecDate;     
        }

        private string AssignColor(string assigned2)
        {
            string status = "";

            switch (assigned2)
            {
                case "RvB PLAST":
                    status = "STATUS_SUSPENDED";
                    break;

                case "RvB 1536":
                    status = "STATUS_ACTIVE";
                    break;

                case "RvB 1836":
                    status = "STATUS_DONE";
                    break;

                case "WaterJet":
                    status = "STATUS_UNDEFINED";
                    break;

                case "EasyJet 4.80":
                    status = "STATUS_4";
                    break;

                case "Scm Record 100":
                    status = "STATUS_2";
                    break;

                case "EasyJet 5.12":
                    status = "STATUS_FAILED";
                    break;

                case "Akron":
                    status = "STATUS_3";
                    break;

                case "Montaggio":
                    status = "STATUS_1";
                    break;

                case "Incollaggio":
                    status = "STATUS_WAITING";
                    break;

                case "Stefani X":
                    status = "STATUS_5";
                    break;

                case "Ufficio tecnico":
                    status = "STATUS_6";
                    break;                    
            }

            return status;
        }

        private string IdGenerator(int x)
        {
            DateTime baseDate = new DateTime(1970, 1, 1);
            DateTime dateTime = Convert.ToDateTime(DateTime.Now.ToShortDateString());

            TimeSpan tempStartDate = dateTime - baseDate;

            string startDate = tempStartDate.TotalMilliseconds.ToString();

            return $"tmp_fk{startDate}_{x}";
        }        


        #endregion

        #region utilities

        private void WriteString2Disk (string inputString, string filename2Write)
        {
            using (StreamWriter sw = new StreamWriter(filename2Write))
            {
                sw.WriteLine(inputString);
            }
        }

        private string ChangeDateFormat(string inputDate)
        {
            string[] parts = inputDate.Split('-');

            return $"{parts[2]}/{parts[1]}/{parts[0]}";
        }

        private List<string> GetGanttAssignementList()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<ProductionCalendarAssignment> assigned = (List<ProductionCalendarAssignment>)dbAccessor.Queryer<ProductionCalendarAssignment>(connectionString, "AssegnaGantt")
                                                            .Where( x=> x.Enabled =="1").ToList();

            List<string> result = assigned.Select(y => y.AssignedTo).ToList();

            return result;
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

        private List<ClienteViewModel> GetCustomers()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<ClienteViewModel> customers = dbAccessor.Queryer<ClienteViewModel>(mesConnectionString, "Clienti");

            return customers;
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

            List<PannelloViewModel> pannelli = (List<PannelloViewModel>)dbAccessor.Queryer<PannelloViewModel>(connectionString, "MagazzinoPannelli")
                                            .Where(x => x.Enabled =="1")
                                            .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();

            List<CollaViewModel> colle = (List<CollaViewModel>)dbAccessor.Queryer<CollaViewModel>(connectionString, "MagazzinoColle")
                                            .Where(x => x.Enabled == "1")
                                            .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();           

            List<BordoViewModel> bordi = (List<BordoViewModel>)dbAccessor.Queryer<BordoViewModel>(connectionString, "MagazzinoBordi")
                                            .Where(x => x.Enabled =="1")
                                            .Where( y => Convert.ToInt32(y.Quantita) <= Convert.ToInt32(y.QuantitaMin)).ToList();                                            

            List<SemilavoratoViewModel>semilavorati = (List<SemilavoratoViewModel>)dbAccessor.Queryer<SemilavoratoViewModel>(connectionString, "MagazzinoSemilavorati")
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

    }
}