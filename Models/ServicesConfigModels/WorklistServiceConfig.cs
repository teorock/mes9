using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ServicesConfigModels
{
    public class WorklistServiceConfig
    {
        public string FileFilter { get; set; }
        public string Folder2Check { get; set; }
        public bool IncludeSubdirectories { get; set; }
        public bool AutoDrive { get; set; }
        public string Logfile { get; set; }
        public string ExcludeString { get; set; }
        public string WorkListCadElementLine { get; set; }
        public string WorkListCadElementLineA1 { get; set; }
        public string WorkListCadElementLineA2 { get; set; }
        public string DbPath { get; set; }
        public string DbTable { get; set; }
        public bool DebugMode { get; set; }
        public bool AutoTimer { get; set; }
        public int AutoTimerLaps { get; set; }
        public bool CheckDoubleInstance { get; set; }
        public int Counter { get; set; }
        public bool Verbose { get; set; }        
    }
}