using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class WorklistProgressViewmodel
    {
        public string MachineName { get; set; }
        public string WorklistName { get; set; }
        public string TotalQuantity { get; set; }
        public string TotalCounter { get; set; }
    }
}