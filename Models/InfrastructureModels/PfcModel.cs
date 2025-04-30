using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class PfcModel
    {
        public long id { get; set; }
        public string NumeroCommessa { get; set; }
        public string Cliente { get; set; }
        public string RifEsterno { get; set; }
        public string DataConsegna { get; set; }
        public string LavorazioniJsonString { get; set; }
        public string Completed { get; set; }
        public string Progress { get; set; }
        public string PfcFiles { get; set; }
        public string Enabled { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}