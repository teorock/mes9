using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class ProductionCalendarPageModel
    {
        public long id { get; set; }
        public string oldEventStart { get; set; }
        public string oldEventEnd { get; set; }
        public string eventTitle { get; set; }
        public string eventDescription { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string operationType { get; set; }
        public string assignedTo { get; set; }
        public string fileLocation { get; set; }
        public string DisplayTotem { get; set; }
    }
}