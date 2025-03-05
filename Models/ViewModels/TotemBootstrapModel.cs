using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class TotemBootstrapModel
    {
        public long id { get; set; }
        public string Title { get; set; }
        public string StartHour { get; set; }
        public string Description { get; set; }
        public string ReferenceDate { get; set; }
        public bool IsDateRepeated { get; set; }
        public int DateRepetition { get; set; }
        public string IsActive {get; set;}
    }
}