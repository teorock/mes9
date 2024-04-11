using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class HourStatistics
    {
        public int Hour { get; set; }
        public double TotalMeters { get; set; }
        public int TotalSides { get; set; }
        public double Thickness { get; set; }
        public double ConsumedMeters { get; set; }
    }
}