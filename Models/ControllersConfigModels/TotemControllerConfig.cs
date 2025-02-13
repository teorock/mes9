using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class TotemControllerConfig
    {
        public string ConnString { get; set; }
        public string Table { get; set; }
        public List<string> TotemEvents { get; set; }  
    }
}