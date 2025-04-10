using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class TotemCalendarEventViewModel
    {
        public long id { get; set; }
        public string EventName { get; set; }
        public string EventDate { get; set; }
        public string EventLink { get; set; }
        public string Enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}