using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mes.ControllerConfig;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace mes.Controllers
{
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        DashboardControllerConfig config = new DashboardControllerConfig();
        private string dashboardControllerConfigPath = @"c:\core\mes\ControllerConfig\ProgramsController.json";

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;

            string rawConf = "";

            using (StreamReader sr = new StreamReader(dashboardControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<DashboardControllerConfig>(rawConf);

        }

        [HttpGet]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        [Route("/Dashboard/DashboardMateriali")]
        [Authorize(Roles ="root, MagMaterialiScrivi")]
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}