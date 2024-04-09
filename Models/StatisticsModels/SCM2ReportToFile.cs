using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class SCM2ReportToFile
    {
        public string Data { get; set; }
        public TimeSpan OraInizioLavoro { get; set; }
        public TimeSpan OraFineLavoro { get; set; }
        public TimeSpan TotaleOreLavoro { get; set; }
        public double TotaleMinutiLavoro { get; set; }
        public double TotaleMetri { get; set; }
        public double TotaleMetriBordoConsumati { get; set; }
        public int TotalePezzi { get; set; }
        public double Spessore { get; set; }
        public double MetriAlMinuto {get; set;}
        public double MetriAllOra { get; set; }
    }
}