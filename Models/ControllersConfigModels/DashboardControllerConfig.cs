using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.ControllerConfig
{
    public class DashboardControllerConfig
    {
        public string ConnectionString { get; set; }
        public string PannelliDbTable { get; set; }
        public string ColleDbTable { get; set; }
        public string SemilavDbTable { get; set; }
        public string BordiDbTable { get; set; }
    }
}