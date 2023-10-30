using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class ProgramsControllerConfig
    {
        public string ConnectionString { get; set; }
        public string MesConnectionString { get; set; }
        public string BordiDbTable { get; set; }            
        public string ColleDbTable { get; set; }
        public string PannelliDbTable { get; set; }
        public string SemilavDbTable { get; set; }
        public string MatPannelliDbTable { get; set; }
        public string MagProdFinitiDbTable { get; set; }
        public string CalendarioDbTable { get; set; }
        public string AssegnaDbTable { get; set; }
        public string ClientiDbTable { get; set; }
        public string ArticoliDbTable { get; set; }
        public string RestiDbTable { get; set; }
    }
}