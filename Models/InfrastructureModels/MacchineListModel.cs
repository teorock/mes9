using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class MacchineListModel
    {
        public long id { get; set; }
        public string MachineName { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}