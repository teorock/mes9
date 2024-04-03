using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class SCM2ReportBody
    {
        public string ProgramName { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Thickness { get; set; }
        public string Passage { get; set; }
        public string EdgeNameLH { get; set; }
        public string EdgeConsumptionLH { get; set; }
        public string EdgeNameRH { get; set; }
        public string DateTime { get; set; }
        public string OperatorName { get; set; }
        public string Workshift { get; set; }        
    }
}