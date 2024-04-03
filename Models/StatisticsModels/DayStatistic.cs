using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class DayStatistic
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ProgramsToday { get; set; }
        public TimeSpan TimeOn { get; set; }
        public TimeSpan TimeWorking { get; set; }
        public double ProgramsPerHour { get; set; }
        public double TotalMeters { get; set; }
        public bool IsAlive { get; set; }  
    }
}