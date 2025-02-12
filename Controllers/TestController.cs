using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using mes.Models.InfrastructureModels;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using mes.Models.ControllersConfig;
using System.IO;
using mes.Models.ControllersConfigModels;
using ServiceStack;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class TestController : Controller
    {
        TestControllerConfig config = new TestControllerConfig();
        const string testControllerConfigPath = @"c:\core\mes\ControllerConfig\TestController.json";
        string authorized = "";
        const string intranetLog=@"c:\temp\intranet.log";        

        public TestController()
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(testControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TestControllerConfig>(rawConf);
            authorized = config.Authorized;       
        }

        //[Authorize(Roles = "root, CalendarOperator, User")]
        [HttpGet]        
        public IActionResult Index(string eventFilter)
        {

            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            

            //leggo configurazione da file e la passo
            string rawConf = "";

            using (StreamReader sr = new StreamReader(testControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TestControllerConfig>(rawConf);

            //UserData userData = GetUserData();
            
            ViewBag.authorize = ((userData.UserRoles.Contains("root")
                                    |userData.UserRoles.Contains("CalendarOperator")
                                    |userData.UserRoles.Contains("CalendarOutdoor"))?true:false).ToString().ToLower();

            ViewBag.defaultView = (userData.UserRoles.Contains("root")
                                    |userData.UserRoles.Contains("CalendarOperator")
                                    |userData.UserRoles.Contains("CalendarOutdoor"))?"dayGridMonth":"dayGridWeek";

            ViewBag.dbContactListener = config.DbContactListener;                
            ViewBag.baseAddress = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            ViewBag.config = config;

            if(eventFilter is null | eventFilter == "")
            {
                ViewBag.eventSourceUrl = config.EventSourceUrl;
            }
            else  if(eventFilter is not null)
            {
                ViewBag.eventSourceUrl = $"{config.EventSourceUrl}{eventFilter}";
            }
            
            //lista dei ruoli di quest'utente
            List<string>tempRoles = userData.UserRoles.Split(',').ToList();
            List<string> userRoles = tempRoles.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x =>x.Trim()).Distinct().ToList();
            //prelevo la lista di tutti i CalendarAssignments
            List<CalendarAssignment> assignments = config.CalendarAssignments;
            //la filtro a seconda del ruolo
            List<string> userAssignments = assignments.Where(x => userRoles.Contains(x.AuthorizedRole)).Select(x => x.AssignmentName).ToList();
            //la passo come ViewBag alla View
            ViewBag.userAssignments = userAssignments;
            ViewBag.eventFilter = eventFilter;

            return View();
        }

        public IActionResult Totem(string eventFilter)
        {
            //string eventFilter = "Merce in Arrivo";
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------
            

            //leggo configurazione da file e la passo
            string rawConf = "";

            using (StreamReader sr = new StreamReader(testControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TestControllerConfig>(rawConf);
            
            //ViewBag.authorize = ((userData.UserRoles.Contains("root")
            //                        |userData.UserRoles.Contains("CalendarOperator")
            //                        |userData.UserRoles.Contains("CalendarOutdoor"))?true:false).ToString().ToLower();
            ViewBag.authorize = false;
//
            //ViewBag.defaultView = (userData.UserRoles.Contains("root")
            //                        |userData.UserRoles.Contains("CalendarOperator")
            //                        |userData.UserRoles.Contains("CalendarOutdoor"))?"dayGridMonth":"dayGridWeek";
            ViewBag.defaultView = "dayGridWeek";

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
            
            ViewBag.eventSourceUrl = $"{config.TotemEventSourceUrl}{eventFilter}";

            //lista dei ruoli di quest'utente
            List<string>tempRoles = userData.UserRoles.Split(',').ToList();
            List<string> userRoles = tempRoles.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x =>x.Trim()).Distinct().ToList();
            //prelevo la lista di tutti i CalendarAssignments
            List<CalendarAssignment> assignments = config.CalendarAssignments;
            //la filtro a seconda del ruolo
            List<string> userAssignments = assignments.Where(x => userRoles.Contains(x.AuthorizedRole)).Select(x => x.AssignmentName).ToList();
            //la passo come ViewBag alla View
            ViewBag.userAssignments = userAssignments;
            ViewBag.eventFilter = eventFilter;

            return View();      
        }

        [Authorize(Roles = "root, CalendarOperator, CalendarOutdoor")]
        [HttpPost]
        public IActionResult InsertEvent(string jsonString)
        {
            UserData userData = GetUserData();
            //--------------------------
            string controllerName = this.ControllerContext.RouteData.Values["controller"].ToString();
            string actionName = this.ControllerContext.RouteData.Values["action"].ToString();                    
            Log2File($"{userData.UserEmail}-->{controllerName},{actionName}");
            //--------------------------            
            ProductionCalendarPageModel getCalendar = new ProductionCalendarPageModel();

            getCalendar = JsonConvert.DeserializeObject<ProductionCalendarPageModel>(jsonString);

            //mode= create, edit, move delete
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            int result = 0;

            //string backgroundColor=GetEventColors(getCalendar.assignedTo).Key;
            //string borderColor = GetEventColors(getCalendar.assignedTo).Value;

            string backgroundColor = config.CalendarAssignments.FirstOrDefault(n => n.AssignmentName == getCalendar.assignedTo).BackgroundColor;
            string borderColor = config.CalendarAssignments.FirstOrDefault(n => n.AssignmentName == getCalendar.assignedTo).BorderColor;

            ProductionCalendarDbModel calendarLine = new ProductionCalendarDbModel {
                id = getCalendar.id,
                title = getCalendar.eventTitle,
                start = getCalendar.startDate,
                end = getCalendar.endDate,
                description = getCalendar.eventDescription,
                assignedTo = getCalendar.assignedTo,
                backgroundColor = backgroundColor,
                borderColor = borderColor,
                fileLocation = getCalendar.fileLocation,
                enabled = "1",
                //allDay = (getCalendar.allDay=="on")? "true": "false",
                CreatedBy = (userData.UserId==null)?"htmlPage": userData.UserEmail,
                CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm")
            };

            switch (getCalendar.operationType)
            {
                case "create":
                    List<ProductionCalendarDbModel> allList = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table);
                    calendarLine.id = allList[allList.Count -1].id + 1;
                    result = dbAccessor.Insertor<ProductionCalendarDbModel>(config.ConnString,config.Table,calendarLine);
                break;

                case "move":
                    result = dbAccessor.Updater<ProductionCalendarDbModel>(config.ConnString,config.Table,calendarLine, getCalendar.id);
                break;

                case "edit":
                    result = dbAccessor.Updater<ProductionCalendarDbModel>(config.ConnString,config.Table,calendarLine, getCalendar.id);
                break;

                case "delete":
                    calendarLine.enabled = "0";
                    result = dbAccessor.Updater<ProductionCalendarDbModel>(config.ConnString,config.Table,calendarLine, getCalendar.id);
                break;                                

            }
            
            return RedirectToAction("Index");
        }

        public string GetAllEvents()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                        .Where(x => x.enabled =="1").ToList();
            
            //var result = JsonConvert.SerializeObject(allEvents);

            List<ProductionCalendarDbModel> rectangular = Rectangulizer(allEvents);

            //var result = JsonConvert.SerializeObject(allEvents);
            var result = JsonConvert.SerializeObject(rectangular);

            return result;
        }

        public string GetEvents(string eventFilter)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = new List<ProductionCalendarDbModel>();
            if(eventFilter is null | eventFilter == "")
            {
                
                allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                            .Where(x => x.enabled =="1").ToList();
            }
            else
            {
                allEvents = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                            .Where(n => n.assignedTo == eventFilter)
                                                            .Where(x => x.enabled =="1").ToList();
            }
            
            //var result = JsonConvert.SerializeObject(allEvents);
            List<ProductionCalendarDbModel> rectangular = Rectangulizer(allEvents);

            //var result = JsonConvert.SerializeObject(allEvents);
            var result = JsonConvert.SerializeObject(rectangular);            

            return result;
        }        

        public string GetEventsTotem(string eventFilter)
        {
            string[] filters = eventFilter.Split(',');

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<ProductionCalendarDbModel> allEvents = new List<ProductionCalendarDbModel>();

            for(int i=0; i< filters.Length; i++)
            {
                allEvents.AddRange(dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString, config.Table)
                                                                .Where(n => n.assignedTo == filters[i])
                                                                .Where(x => x.enabled =="1").ToList());
            }
            
            List<ProductionCalendarDbModel> rectangular = Rectangulizer(allEvents);

            //var result = JsonConvert.SerializeObject(allEvents);
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

        public IActionResult OpenSmbFolder(string path2open)
        {            
           
            //string escapedPath = Uri.EscapeDataString($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/Test/OpenSmbFolder?path2open={path2open}");
            string escaped = EscapeBackslashes(path2open);
            string script = $"window.open('{escaped}');";
            return Content(script, "application/javascript");
        }

        private string EscapeBackslashes(string input)
        {
            return input.Replace("\\", "\\\\");
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