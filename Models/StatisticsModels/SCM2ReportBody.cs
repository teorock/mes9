using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class SCM2ReportBody
    {
        public string ProgramName { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public int Passage { get; set; }
        public string EdgeNameLH { get; set; }
        public double EdgeConsumptionLH { get; set; }
        public string EdgeNameRH { get; set; }
        public DateTime DateTime { get; set; }
        public string OperatorName { get; set; }
        public string Workshift { get; set; }        
    }
}