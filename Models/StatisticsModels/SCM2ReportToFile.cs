using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class SCM2ReportToFile
    {
        public DateTime Data { get; set; }
        public TimeSpan OraInizioLavoro { get; set; }
        public TimeSpan OraFineLavoro { get; set; }
        public TimeSpan TotaleOreLavoro { get; set; }
        public TimeSpan TotaleMinutiLavoro { get; set; }
        public double TotaleMetri { get; set; }
        public double TotaleMetriBordoConsumati { get; set; }
        public int TotalePezzi { get; set; }
        public double Spessore { get; set; }
    }
}