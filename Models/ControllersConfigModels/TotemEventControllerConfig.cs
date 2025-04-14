using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class TotemEventControllerConfig
    {
        public string ConnString { get; set; }
        public string EventTable { get; set; }   
        public string IntranetLog { get; set; }    
        public string DbContactListener { get; set; } 
    }
}