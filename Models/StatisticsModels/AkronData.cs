using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class AkronData
    {
        public DateTime Date { get; set; }
        public int Code { get; set; }
        public string? Descr { get; set; }
        public string? A { get; set; }
        public string? B { get; set; }
        public string? C { get; set; }
        public string? D { get; set; }
        public string? E { get; set; }
        public string? F { get; set; }
        public string? G { get; set; }
        public double TotalMeters { get; set; }
        public int TotalPanels { get; set; }        
    }
}