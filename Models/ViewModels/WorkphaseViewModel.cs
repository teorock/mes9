using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;

namespace mes.Models.ViewModels
{
    public class WorkphaseViewModel
    {
        public long id { get; set; }
        public long RelatedWorkOrder { get; set; }
        public string WorkphaseName { get; set; }
        public string Completed { get; set; }
        public string QualityCheck { get; set; }
        public string QualityOperator { get; set; }        

    }
}