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

namespace mes.Controllers
{
    public class TotemEventsController : Controller
    {
        // Use forward slashes for SQLite connection string
        const string connString = "Data Source=C:\\core\\mesdata\\erpdata.db";
        const string eventTable = "EventCalendar";
        const string defaultBackgroundColor = "#3788d8";
        const string defaultBorderColor = "#2C6EB5";

        /// <summary>
        /// Displays the main calendar view
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // Set base properties for the calendar view
            ViewBag.dbContactListener = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/TotemEvents/InsertEvent";
            ViewBag.eventSourceUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/TotemEvents/GetEvents";
            ViewBag.baseAddress = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
            
            // Allow editing for all users (simplified approach)
            ViewBag.authorize = "true";
            ViewBag.defaultView = "dayGridMonth";
            
            return View();
        }

        /// <summary>
        /// Handles the creation, update, and deletion of events
        /// </summary>
        [HttpPost]
        public IActionResult InsertEvent(string jsonString)
        {
            // Deserialize the event data
            EventCalendarViewModel eventData = JsonConvert.DeserializeObject<EventCalendarViewModel>(jsonString);
            
            // Access the database
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            int result = 0;
            
            // Create the event model for database operations
            EventCalendarModel calendarEvent = new EventCalendarModel
            {
                Id = eventData.Id,
                Title = eventData.Title,
                Start = eventData.Start,
                End = eventData.End,
                Description = eventData.Description,
                BackgroundColor = defaultBackgroundColor,
                BorderColor = defaultBorderColor,
                Enabled = "1",
                CreatedBy = GetUserEmail(),
                CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            };
            
            // Perform the appropriate database operation based on the operation type
            switch (eventData.OperationType)
            {
                case "create":
                    // Get all events to find the next available ID
                    List<EventCalendarModel> allEvents = dbAccessor.Queryer<EventCalendarModel>(connString, eventTable);
                    calendarEvent.Id = allEvents.Count > 0 ? allEvents.Max(e => e.Id) + 1 : 1;
                    result = dbAccessor.Insertor<EventCalendarModel>(connString, eventTable, calendarEvent);
                    break;
                
                case "edit":
                case "move":
                    result = dbAccessor.Updater<EventCalendarModel>(connString, eventTable, calendarEvent, eventData.Id);
                    break;
                
                case "delete":
                    calendarEvent.Enabled = "0";
                    result = dbAccessor.Updater<EventCalendarModel>(connString, eventTable, calendarEvent, eventData.Id);
                    break;
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Returns all events from the database as JSON
        /// </summary>
        [HttpGet]
        public IActionResult GetEvents()
        {
            try
            {               
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                List<EventCalendarModel> allEvents = dbAccessor.Queryer<EventCalendarModel>(connString, eventTable)
                                                    .Where(x => x.Enabled == "1").ToList();
                
                // Convert to FullCalendar format
                var calendarEvents = ConvertToFullCalendarFormat(allEvents);
                
                // Use camelCase for JSON property names (FullCalendar expects this)
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
        
        /// <summary>
        /// Converts database model to FullCalendar-compatible format
        /// </summary>
        private List<CalendarEventDto> ConvertToFullCalendarFormat(List<EventCalendarModel> events)
        {
            var result = new List<CalendarEventDto>();
            
            foreach (EventCalendarModel evt in events)
            {
                try
                {
                    DateTime startDate = DateTime.Parse(evt.Start);
                    DateTime endDate = DateTime.Parse(evt.End);
                    
                    // Create a FullCalendar-compatible event object
                    var calendarEvent = new CalendarEventDto ()
                    {   
                        Id = evt.Id,
                        Start = startDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        End = endDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        Title = evt.Title,
                        Description = evt.Description ?? "",
                        BackgroundColor = !string.IsNullOrEmpty(evt.BackgroundColor) ? evt.BackgroundColor : defaultBackgroundColor,
                        BorderColor = !string.IsNullOrEmpty(evt.BorderColor) ? evt.BorderColor : defaultBorderColor,
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
        
        /// <summary>
        /// Gets the current user's email address
        /// </summary>
        private string GetUserEmail()
        {
            return User.FindFirstValue(ClaimTypes.Email) ?? "anonymous";
        }
    }
    
    /// <summary>
    /// <summary>
    /// View model for event data coming from the client
    /// </summary>
    public class EventCalendarViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Description { get; set; }
        public string OperationType { get; set; } // create, edit, move, delete
    }
    
    /// <summary>
    /// Data transfer object for FullCalendar events
    /// </summary>
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
