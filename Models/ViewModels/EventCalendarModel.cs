using System;
using System.ComponentModel.DataAnnotations;

namespace mes.Models.ViewModels
{

    public class EventCalendarModel
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Description { get; set; }
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public string Enabled { get; set; } = "1";
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}

