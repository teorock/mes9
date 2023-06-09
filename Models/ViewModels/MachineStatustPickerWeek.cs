using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class MachineStatustPickerWeek
    {
        public string Day { get; set; }
        public string Start { get; set; }
        public string Waiting { get; set; }
        public string ManualMovements { get; set; }
        public string Connected { get; set; }
        public string NotConnected { get; set; }
        public string Emergency { get; set; }
    }
}