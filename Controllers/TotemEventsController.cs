using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using mes.Models.ViewModels;
using mes.Models.Services.Infrastructures;
using System.IO;
using Newtonsoft.Json.Serialization;
using mes.Models.ControllersConfigModels;
using Microsoft.Extensions.Logging;
using mes.Models.InfrastructureModels;
using Microsoft.AspNetCore.Authorization;

namespace mes.Controllers
{
    public class TotemEventsController : Controller
    {
        private readonly ILogger<TotemEventsController> _logger;
        TotemEventsControllerConfig config = new TotemEventsControllerConfig();
        const string totemEventsControllerConfigPath = @"c:\core\mes\ControllerConfig\TotemEventsControllerConfig.json";
        const string intranetLog = @"c:\temp\intranet.log";         

        public TotemEventsController(ILogger<TotemEventsController> logger)
        {
            _logger = logger;
            string rawConf = "";

            using (StreamReader sr = new StreamReader(totemEventsControllerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<TotemEventsControllerConfig>(rawConf);            
        }

        [HttpGet]
        [Authorize(Roles = "root, TotemEventOperator, User")]
        public IActionResult Index()
        {
            //controlla presenza eventi per oggi
            //setta una viewbag message
            List<string> messages = GetTodayMessage();
            ViewBag.messages = messages;

            ViewBag.dbContactListener = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/TotemEvents/InsertEvent";
            ViewBag.eventSourceUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/TotemEvents/GetEvents";
            ViewBag.baseAddress = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            
            ViewBag.authorize = "true";
            ViewBag.defaultView = "dayGridMonth";
            
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, TotemEventOperator, User")]
        public IActionResult InsertEvent(string jsonString)
        {
            EventCalendarViewModel eventData = JsonConvert.DeserializeObject<EventCalendarViewModel>(jsonString);

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            int result = 0;
            
            EventCalendarModel calendarEvent = new EventCalendarModel
            {
                Id = eventData.Id,
                Title = eventData.Title,
                Start = eventData.Start,
                End = eventData.End,
                Description = eventData.Description,
                BackgroundColor = config.DefaultBackgroundColor,
                BorderColor = config.DefaultBorderColor,
                Enabled = "1",
                CreatedBy = GetUserEmail(),
                CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };
            
            switch (eventData.OperationType)
            {
                case "create":
                    List<EventCalendarModel> allEvents = dbAccessor.Queryer<EventCalendarModel>(config.ConnString, config.EventTable);
                    calendarEvent.Id = allEvents.Count > 0 ? allEvents.Max(e => e.Id) + 1 : 1;
                    result = dbAccessor.Insertor<EventCalendarModel>(config.ConnString, config.EventTable, calendarEvent);
                    if(config.AutoInsertToLogisticDb == 1) //modifica per inserimento nella "riga sotto" del totem logistica
                    {
                        //bisogna mappare EventCalendarModel to ProductionCalendarViewModel
                        SyncCalendars(calendarEvent);
                    }
                    break;
                
                case "edit":
                case "move":
                    result = dbAccessor.Updater<EventCalendarModel>(config.ConnString, config.EventTable, calendarEvent, eventData.Id);
                    if(config.AutoInsertToLogisticDb == 1) //modifica per inserimento nella "riga sotto" del totem logistica
                    {
                        //bisogna mappare EventCalendarModel to ProductionCalendarViewModel
                        SyncCalendars(calendarEvent);
                    }                    
                    break;
                
                case "delete":
                    calendarEvent.Enabled = "0";
                    result = dbAccessor.Updater<EventCalendarModel>(config.ConnString, config.EventTable, calendarEvent, eventData.Id);
                    break;
            }
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult TotalSyncCalendar()
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<EventCalendarModel> allCalendarEvent = dbAccessor.Queryer<EventCalendarModel>(config.ConnString, config.EventTable);
            foreach(EventCalendarModel oneEvent in allCalendarEvent)
            {
                SyncCalendars(oneEvent);
            }

            return RedirectToAction("Index");
        }
        private void SyncCalendars(EventCalendarModel inputModel)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            long lastId = dbAccessor.Queryer<ProductionCalendarDbModel>(config.ConnString,config.ProductionTable)
                                    .Select(i => i.id).Max();

            ProductionCalendarDbModel mapped = new ProductionCalendarDbModel(){
                id = lastId + 1,
                title = inputModel.Title,
                start = inputModel.Start,
                end = inputModel.End,
                description = inputModel.Description,
                assignedTo = config.AssignedToString,
                backgroundColor = inputModel.BackgroundColor,
                borderColor = inputModel.BorderColor,
                fileLocation = "---",
                displayTotem = "1",
                enabled = inputModel.Enabled,
                CreatedBy = inputModel.CreatedBy,
                CreatedOn = inputModel.CreatedOn
            };
            int result = dbAccessor.Insertor<ProductionCalendarDbModel>(config.ConnString,config.ProductionTable,mapped);
        }


        [HttpGet]
        [Authorize(Roles = "root, TotemEventOperator, User")]
        public IActionResult GetEvents()
        {
            try
            {               
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                List<EventCalendarModel> allEvents = dbAccessor.Queryer<EventCalendarModel>(config.ConnString, config.EventTable)
                                                    .Where(x => x.Enabled == "1").ToList();
                
                var calendarEvents = ConvertToFullCalendarFormat(allEvents);
                
                var serializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                
                var json = JsonConvert.SerializeObject(calendarEvents, serializerSettings);
                System.Diagnostics.Debug.WriteLine($"Returning JSON: {json}");
                
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetEvents: {ex.Message}");
                return Content("[]", "application/json");
            }
        }
        
        public List<string> GetTodayMessage()
        {
            //start 2025-04-15T08:00
            //end 2025-04-15T09:00
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<string> message = new List<string>();
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            
            List<EventCalendarModel> allEvents = dbAccessor.Queryer<EventCalendarModel>(config.ConnString, config.EventTable)
                                                .Where(t => t.Start.Contains(today))
                                                .Where(x => x.Enabled == "1").ToList();

            if(allEvents.Count!=0) message = allEvents.Select(m => m.Description).ToList();
            
            return message;
        }

        private List<CalendarEventDto> ConvertToFullCalendarFormat(List<EventCalendarModel> events)
        {
            var result = new List<CalendarEventDto>();
            
            foreach (EventCalendarModel evt in events)
            {
                try
                {
                    DateTime startDate = DateTime.Parse(evt.Start);
                    DateTime endDate = DateTime.Parse(evt.End);
                    
                    var calendarEvent = new CalendarEventDto ()
                    {   
                        Id = evt.Id,
                        Start = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        End = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        Title = evt.Title,
                        Description = evt.Description ?? "",
                        BackgroundColor = !string.IsNullOrEmpty(evt.BackgroundColor) ? evt.BackgroundColor : config.DefaultBackgroundColor,
                        BorderColor = !string.IsNullOrEmpty(evt.BorderColor) ? evt.BorderColor : config.DefaultBorderColor,
                        AllDay = false
                    };
                    
                    result.Add(calendarEvent);
                    System.Diagnostics.Debug.WriteLine($"Converted event: {calendarEvent.Id} - {calendarEvent.Title} ({calendarEvent.Start} to {calendarEvent.End})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error converting event {evt.Id}: {ex.Message}");
                    continue;
                }
            }
            
            return result;
        }
        
        private string GetUserEmail()
        {
            return User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
        }
    }
    

    public class EventCalendarViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Description { get; set; }
        public string OperationType { get; set; } // create, edit, move, delete
    }
    
    public class CalendarEventDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Description { get; set; }
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public bool AllDay { get; set; }
    }
}
