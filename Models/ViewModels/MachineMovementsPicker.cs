using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class MachineMovementsPicker
    {
        public long id { get; set; }
        public string MachineName { get; set; }
        public string WorklistName { get; set; }
        public string ProgramName { get; set; }
        public string OperationType { get; set; }
        public string PropName { get; set; }
        public string NewValue { get; set; }
        public string PrevValue { get; set; }
        public string ProgramUri { get; set; }
        public string Counter { get; set; } //eseguiti
        public string Quantity { get; set; } // richiesti
        public string CreatedOn {get; set;}        
    }
}