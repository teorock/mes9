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
        public bool AutoScroll { get; set; }
        public int FirstSlideStart { get; set; }
        public int StandSlideTime { get; set; }
        public int BetweenSlideTime { get; set; }
        public string TotemEventsConnString { get; set; }
        public string TotemEventsDbTable { get; set; }
        public int EnableEventStrip { get; set; }
    }
}