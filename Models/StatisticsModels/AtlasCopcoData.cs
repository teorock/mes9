using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.StatisticsModels
{
    public class AtlasCopcoData
    {
        public string id { get; set; }
        public int componentIdx { get; set; }
        public int propertyIdx { get; set; }
        public string componentName { get; set; }
        public string propertyName { get; set; }
        public string type { get; set; }
        public string unit { get; set; }
        public string limits { get; set; }
        public int minAllowedTierRead { get; set; }
        public int minAllowedTierWrite { get; set; }
        public string icon { get; set; }
        public List<string> tags { get; set; }
        public string componentType { get; set; }
        public string description { get; set; }
        public string format { get; set; }
        public double value { get; set; }
        public bool isEnergyTrend { get; set; }
        public int slot { get; set; }        
    }
}