using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class ProductionCalendarDbModel
    {
        public long id { get; set; }
        public string title { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string description { get; set; }
        public string assignedTo { get; set; }
        public string backgroundColor { get; set; }
        public string borderColor { get; set; }
        public string fileLocation { get; set; }
        public string displayTotem { get; set; }
        public string enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}