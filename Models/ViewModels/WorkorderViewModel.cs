using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class WorkorderViewModel
    {
        public long id { get; set; }
        public string Customer { get; set; }
        public string WorkNumber { get; set; }
        public DateTime Delivery { get; set; }
        public string Description { get; set; }
        public string ExternalRef { get; set; }
        public List<WorkphaseViewModel> WorkPhases { get; set; }
    }
}