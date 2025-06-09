using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class PfcControllerConfig
    {
        public string ConnString { get; set; }
        public string ConnString2 { get; set; }
        public string PfcConnString { get; set; }
        public string CustomerTable { get; set; }
        public string OperatorsTable { get; set; }
        public string WorkphaseTable { get; set; }
        public string PfcTable { get; set; }
        public string CsvDaneaTable { get; set; }
        public string CsvFilter1 { get; set; }
        public string CsvFilter2 { get; set; }
        public string BaseUploadFolder { get; set; }
        public string CsvOrdiniClienteCheck { get; set; }
    }
}