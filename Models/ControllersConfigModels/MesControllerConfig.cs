using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class MesControllerConfig
    {
        public string LastInstantConnString {get; set; }
        public string LastPeriodConnString {get; set; }
        public string ConnectionString {get; set; }
    }
}