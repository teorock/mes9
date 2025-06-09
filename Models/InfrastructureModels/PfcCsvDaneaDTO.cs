using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class PfcCsvDaneaDTO
    {
        public string Data { get; set; }
        public string NCommessa { get; set; }
        public string Cliente { get; set; }
        public string Stato { get; set; }
        public string DataConsegna { get; set; }
        public string Taken { get; set; }      
        public string PfcNumber { get; set; }          
    }
}