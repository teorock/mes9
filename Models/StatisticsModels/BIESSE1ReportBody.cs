using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class BIESSE1ReportBody
    {
        public string Giorno { get; set; }
        public string OraInizio { get; set; }
        public string OraFine { get; set; }
        public string TempoLavoro { get; set; }
        public string TempoAccensione { get; set; }
        public string TempoTotale { get; set; }
        public string MinutiLavoro { get; set; }
        public string MinutiAccensione { get; set; }
        public string MinutiTotali { get; set; }
        public int ProgrammiEseguiti { get; set; }
        public double ProgrammiOra { get; set; }

    }
}