using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class TotemEventsControllerConfig
    {
        public string ConnString { get; set; }
        public string EventTable { get; set; }
        public string DefaultBackgroundColor { get; set; }
        public string DefaultBorderColor { get; set; }
        public int RefreshInterval { get; set; }
    
    }
}