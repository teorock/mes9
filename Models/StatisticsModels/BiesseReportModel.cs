using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class BiesseReportModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Worklist { get; set; }
        public string Program { get; set; }
        public TimeSpan Time { get; set; }
        public int Origin { get; set; }
        public int Result { get; set; }
        public int TotalPieces { get; set; } //numero totale dei pezzi, contando i nesting e i non nesting
        public bool HasNesting { get; set; } //in questa giornata sono stati fatti i nesting
        public int ReportLines { get; set; } //numero di righe del file di report (se non nesting, uguali al numero di pezzi fatti)
    }
}