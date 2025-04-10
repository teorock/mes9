using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using mes.Models.Services.Infrastructures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mes.Models.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class TotemCalendarEvents : Controller
    {
        private readonly ILogger<TotemCalendarEvents> _logger;
        const string connectionString = "Data Source=c:\\core\\mesData\\TotemCalendarEvents.db";

        public TotemCalendarEvents(ILogger<TotemCalendarEvents> logger)
        {
            _logger = logger;
        }

        #region TotemCalendarEvents

        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult Index()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<TotemCalendarEventViewModel> TotemCalendarEvents = dbAccessor.Queryer<TotemCalendarEventViewModel>(connectionString, "Events")
                                            .Where(x => x.Enabled =="1").ToList();

            return View(TotemCalendarEvents);

        }

        [Authorize(Roles = "root")]
        public IActionResult AggiornaCalendarEvents(List<TotemCalendarEventViewModel> TotemCalendarEvents)
        {
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(TotemCalendarEventViewModel oneModel in TotemCalendarEvents)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    int result = dbAccessor.Updater<TotemCalendarEventViewModel>(connectionString, "Events", oneModel, oneModel.id);
                }
            return RedirectToAction("MainTotemCalendarEvents");
        }

        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult InsertCalendarEvent()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<TotemCalendarEventViewModel> TotemCalendarEvents = dbAccessor.Queryer<TotemCalendarEventViewModel>(connectionString, "Events")
                                        .Where(x => x.Enabled=="1").ToList();            
            
            ViewBag.TotemCalendarEventsList = TotemCalendarEvents;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult InsertCalendarEvent(TotemCalendarEventViewModel newTotemCalendarEvent)
        {
            UserData userData = GetUserData();

            newTotemCalendarEvent.CreatedBy = userData.UserName;
            newTotemCalendarEvent.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newTotemCalendarEvent.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<TotemCalendarEventViewModel> TotemCalendarEvents = dbAccessor.Queryer<TotemCalendarEventViewModel>(connectionString, "Events");

            //long max = (from l in TotemCalendarEvents select l.id).Max();
            if(TotemCalendarEvents.Count() > 0)
            {
                long max = (from l in TotemCalendarEvents select l.id).Max();
                newTotemCalendarEvent.id = max + 1;
            } else {
                newTotemCalendarEvent.id = 0;
            }

            //newTotemCalendarEvent.id = max + 1;

            int result = dbAccessor.Insertor<TotemCalendarEventViewModel>(connectionString, "Events", newTotemCalendarEvent);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "root")]
        public IActionResult ModCalendarEvent(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<TotemCalendarEventViewModel> TotemCalendarEvents = (List<TotemCalendarEventViewModel>)dbAccessor.Queryer<TotemCalendarEventViewModel>(connectionString, "Events")
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.TotemCalendarEventsList = TotemCalendarEvents;
            TotemCalendarEventViewModel oneModel = TotemCalendarEvents.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root")]
        public IActionResult ModCalendarEvent(TotemCalendarEventViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<TotemCalendarEventViewModel>(connectionString,"Events", oneModel, oneModel.id);

            return RedirectToAction("MainTotemCalendarEvents");
        }        


        #endregion

        private UserData GetUserData()
        {
            UserData userData = new UserData();

            string userRoles="";
            ViewBag.userId =  User.FindFirstValue(ClaimTypes.NameIdentifier); 
            userData.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.userName =  User.FindFirstValue(ClaimTypes.Name); 
            userData.UserName = User.FindFirstValue(ClaimTypes.Name);

            IEnumerable<Claim> roles = User.FindAll(ClaimTypes.Role);
            foreach(var role in roles)
            {
                userRoles += $"{role.Value}, ";
            }
            userData.UserRoles = userRoles;
            
            ViewBag.userEmail =  User.FindFirstValue(ClaimTypes.Email); 
            userData.UserEmail = User.FindFirstValue(ClaimTypes.Email);

            ViewBag.userRoles= userRoles;                    
        
            ViewBag.address = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            ViewBag.port = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            userData.UserIpAddress = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            userData.UserIpPort = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            return userData;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}