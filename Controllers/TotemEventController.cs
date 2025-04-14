using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.InfrastructureModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class TotemEventController : Controller
    {
        private readonly ILogger<TotemEventController> _logger;
        TotemEventControllerConfig config = new TotemEventControllerConfig();
        string totemEventControllerConfigPath = @"c:\core\mes\ControllerConfig\TotemEventController.json";

        public TotemEventController(ILogger<TotemEventController> logger)
        {
            _logger = logger;
            string rawConf = "";

            using (StreamReader sr = new StreamReader(totemEventControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TotemEventControllerConfig>(rawConf);            
        }

        public IActionResult Index()
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            

            //leggo configurazione da file e la passo
            string rawConf = "";

            using (StreamReader sr = new StreamReader(totemEventControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TotemEventControllerConfig>(rawConf);

            //UserData userData = GetUserData();
            
            ViewBag.authorize = ((userData.UserRoles.Contains("root")
                                    |userData.UserRoles.Contains("CalendarOperator")
                                    |userData.UserRoles.Contains("CalendarOutdoor")
                                    |userData.UserRoles.Contains("CalendarEventsManager"))?true:false).ToString().ToLower();

            ViewBag.defaultView = (userData.UserRoles.Contains("root")
                                    |userData.UserRoles.Contains("CalendarOperator")
                                    |userData.UserRoles.Contains("CalendarOutdoor")
                                    |userData.UserRoles.Contains("CalendarEventsManager"))?"dayGridMonth":"dayGridWeek";

            ViewBag.dbContactListener = config.DbContactListener;                
            ViewBag.baseAddress = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            ViewBag.config = config;

            //if(eventFilter is null | eventFilter == "")
            //{
            //    ViewBag.eventSourceUrl = config.EventSourceUrl;
            //}
            //else  if(eventFilter is not null)
            //{
            //    ViewBag.eventSourceUrl = $"{config.EventSourceUrl}{eventFilter}";
            //}
            //
            //lista dei ruoli di quest'utente
            List<string>tempRoles = userData.UserRoles.Split(',').ToList();
            List<string> userRoles = tempRoles.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x =>x.Trim()).Distinct().ToList();
            //prelevo la lista di tutti i CalendarAssignments
            //List<CalendarAssignment> assignments = config.CalendarAssignments;
            //la filtro a seconda del ruolo
            //List<string> userAssignments = assignments.Where(x => userRoles.Contains(x.AuthorizedRole)).Select(x => x.AssignmentName).ToList();
            //la passo come ViewBag alla View
            //ViewBag.userAssignments = userAssignments;
            //ViewBag.eventFilter = eventFilter;

            return View();

        }

        public string GetAllEvents()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.EventTable)
                                                        .Where(x => x.enabled =="1").ToList();            

            List<ProductionCalendarDbModel> rectangular = Rectangulizer(allEvents);

            var result = JsonConvert.SerializeObject(rectangular);

            return result;
        }

        public string GetEvents(string eventFilter)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = new List<ProductionCalendarDbModel>();
            if(eventFilter is null | eventFilter == "")
            {
                
                allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.EventTable)
                                                            .Where(x => x.enabled =="1").ToList();
            }
            else
            {
                allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.EventTable)
                                                            .Where(n => n.assignedTo == eventFilter)
                                                            .Where(x => x.enabled =="1").ToList();
            }
            
            List<ProductionCalendarDbModel> rectangular = Rectangulizer(allEvents);

            var result = JsonConvert.SerializeObject(rectangular);            

            return result;
        }        

        private List<ProductionCalendarDbModel> Rectangulizer(List<ProductionCalendarDbModel> inputList)
        {
            List<ProductionCalendarDbModel> result = new List<ProductionCalendarDbModel>();

            foreach(ProductionCalendarDbModel oneModel in inputList)
            {
                DateTime startDate = Convert.ToDateTime(oneModel.start);
                DateTime endDate = Convert.ToDateTime(oneModel.end);

                if(startDate == endDate)
                {
                    oneModel.start = startDate.ToString("yyyy-MM-dd");
                    oneModel.end = endDate.ToString("yyyy-MM-dd");
                    result.Add(oneModel);
                }
                else result.Add(oneModel);
            }

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

        private void Log2File(string line2log)
        {
            using(StreamWriter sw = new StreamWriter(config.IntranetLog, true))
            {
                sw.WriteLine($"{DateTime.Now} -> {line2log}");
            }
        }  

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}