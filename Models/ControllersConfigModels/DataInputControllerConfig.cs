using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class DataInputControllerConfig
    {
        public string ConnString { get; set; }
        public string ArticlesDbTable { get; set; }
        public string CustomersDbTable { get; set; }
        public string PanelsConnString { get; set; }
        public string PanelsTable { get; set; }
        public string SemilavConnString { get; set; }
        public string SemilavDbTable { get; set; }
    }
}