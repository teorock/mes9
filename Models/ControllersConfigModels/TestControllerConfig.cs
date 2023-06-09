using System.Collections.Generic;
using mes.Models.ControllersConfigModels;

namespace mes.Models.ControllersConfig
{
    public class TestControllerConfig
    {
        public string ConnString { get; set; }
        public string Table { get; set; }
        public string EventSourceUrl { get; set; }
        public string DbContactListener { get; set; }
        public List<CalendarAssignment> CalendarAssignments { get; set; }                                                                                      
    }
}