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
    }
}