using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class SCM1ReportModel
    {
        public DateTime ReferenceDay { get; set; }
        public string Area { get; set; }
        public string FilePGM { get; set; }
        public double DimX { get; set; }
        public double DimY { get; set; }
        public double DimZ { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan Stop { get; set; }
        public TimeSpan Teffettivo { get; set; }
        public TimeSpan Ttotale { get; set; }
        public int Quantita { get; set; }
        public TimeSpan Tmedio { get; set; }
        public string Descrizione { get; set; }        
    }
}