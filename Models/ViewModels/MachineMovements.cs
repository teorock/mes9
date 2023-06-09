using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class MachineMovements
    {
        public string MachineName { get; set; }
        public string MachineStatus { get; set; }
        public string MachineAlarm { get; set; }
        public string WorklistName { get; set; }
        public string ProgramName { get; set; }
        public string Counter { get; set; }
        public string Quantity { get; set; }
        public string CreatedOn { get; set; }
    }
}