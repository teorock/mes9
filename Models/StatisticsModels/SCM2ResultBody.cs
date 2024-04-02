using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureConfigModels
{
    public class SCM2ResultBody
    {
        public string Work { get; set; }
        public string Alarms { get; set; }
        public string Warnings { get; set; }
        public string TrackSpeed { get; set; }
        public string Empty { get; set; }
        public int PiecesInMachine { get; set; }
        public string AxesZero { get; set; }
        public string OrderStatus { get; set; }
        public string User { get; set; }
        public string Type { get; set; }
        public string DateTime { get; set; }
    }        
}
