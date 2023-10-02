using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class ProductionController : Controller
    {
        private readonly ILogger<ProductionController> _logger;
        ProductionControllerConfig config = new ProductionControllerConfig();
        const string prodControllerConfigPath = @"c:\core\mes\ControllerConfig\ProductionController.json";

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

        [HttpGet]
        [Authorize(Roles ="root")]
        public IActionResult Index()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> requests = dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                                .Where(e => e.Enabled == "1").ToList();

            return View(requests);
        }

        [Authorize(Roles = "root")]
        public IActionResult AggiornaProductionRequests(List<ProductionRequest> ProductionRequests)
        {
               
            UserData userData = GetUserData();
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
        [Authorize(Roles = "root")]
        public IActionResult InsertProductionRequest()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> ProductionRequests = (List<ProductionRequest>)dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.ProductionRequestsList = ProductionRequests;

            //ViewBag.clienti
            //ViewBag.articoli

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult InsertProductionRequest(ProductionRequest newProductionRequest)
        {
            UserData userData = GetUserData();

            newProductionRequest.CreatedBy = userData.UserName;
            newProductionRequest.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newProductionRequest.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> ProductionRequests = (List<ProductionRequest>)dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable);

            long max = (from l in ProductionRequests select l.id).Max();

            newProductionRequest.id = max + 1;

            int result = dbAccessor.Insertor<ProductionRequest>(config.ConnString, config.DbTable, newProductionRequest);

            return RedirectToAction("Index");
        }


        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult ModProductionRequest(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionRequest> ProductionRequests = (List<ProductionRequest>)dbAccessor.Queryer<ProductionRequest>(config.ConnString, config.DbTable)
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.ProductionRequestsList = ProductionRequests;
            ProductionRequest oneModel = ProductionRequests.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult ModProductionRequest(ProductionRequest oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<ProductionRequest>(config.ConnString, config.DbTable, oneModel, oneModel.id);

            return RedirectToAction("MainProductionRequests");
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

    }
}