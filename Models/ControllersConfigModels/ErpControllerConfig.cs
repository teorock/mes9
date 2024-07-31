using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class ErpControllerConfig
    {
        public string ConnectionString { get; set; }
        public string UserDbConnString { get; set; }
        public string IntranetLog { get; set; }
        public string DateFormat { get; set; }
    }
}