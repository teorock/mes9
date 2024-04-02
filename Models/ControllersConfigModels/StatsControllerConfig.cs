using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class StatsControllerConfig
    {
        public List<MachineDetails> AvailableMachines { get; set; }
    }

    public class MachineDetails
    {
        public string MachineName { get; set; }
        public string MachineType { get; set; }
        public string RetrieveMethod { get; set; }
        public string DataTypes { get; set; }
        public string ServerAddress { get; set; }
        public int ServerPort { get; set; }
        public string UserName  { get; set; }
        public string WebGetString { get; set; }
        public int DaysDisplayedDefault { get; set; }
    }
}