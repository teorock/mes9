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

namespace intranet.Controllers
{
    //[Route("[controller]")]
    public class DataInputController : Controller
    {
        private readonly ILogger<DataInputController> _logger;
        private readonly string connectionString="Data Source=../mesData/datasource.db";
        private readonly UserManager<IdentityUser> _userManager;        

        public DataInputController(ILogger<DataInputController> logger)
        {
            _logger = logger;
        }
      
        public IActionResult AnagraficheMain()
        {
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
            List<ClienteViewModel> allCustomers = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(connectionString, "Clienti");
                        
            return View("Customers", allCustomers);
        }
        
        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult ModCustomer(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(connectionString, "Clienti"); 
            
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

            int result = dbAccessor.Updater<ClienteViewModel>(connectionString,"Clienti", oneModel, oneModel.id);

            return RedirectToAction("Customers");
        }        

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult InsertCustomer()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(connectionString, "Clienti");            
            
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
            List<ClienteViewModel> clienti = (List<ClienteViewModel>)dbAccessor.Queryer<ClienteViewModel>(connectionString, "Clienti");      

            long max = (from l in clienti select l.id).Max();

            newCustomer.id = max + 1;
            GeneralPurpose genPurp = new GeneralPurpose();

            int result = dbAccessor.Insertor<ClienteViewModel>(connectionString, "Clienti", genPurp.DenullifyObj<ClienteViewModel>(newCustomer));

            return RedirectToAction("Customers");
        }


#endregion

 

        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult Orders()
        {
            //commesse o ordini
            return View("Orders");
        }
        
        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult Materials()
        {
            //materiali
            return View("Materials");
        }    
        
        [HttpGet]
        [Authorize(Roles = "root, DataInput")]
        public IActionResult ProcessType()
        {
            //tipi di lavorazioni
            return View("ProcessType");
        }            
        
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
    }
}