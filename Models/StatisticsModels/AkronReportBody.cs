using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class AkronReportBody
    {
        public DateTime Data { get; set; }
        public TimeSpan OraInizio { get; set; }
        public TimeSpan OraFine { get; set; }
        public int TotaleLati { get; set; }
        public double TotaleMetri { get; set; }
        public string Spessori { get; set; }
    }
}