using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ControllersConfigModels
{
    public class MesControllerConfig
    {
        public string LastInstantConnString {get; set; }
        public string LastPeriodConnString {get; set; }
        public string ConnectionString {get; set; }
        public string WorklistFileRetrieveMode { get; set; }
        public string FtpServer { get; set; }
        public string FtpUser { get; set; }
        public string FtpLocalDestination { get; set; }
    }
}