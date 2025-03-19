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
        public string CustomerTable { get; set; }
        public string OperatorsTable { get; set; }     
        public string WorkphaseTable { get; set; }
        public string PfcTable { get; set; }
    }
}