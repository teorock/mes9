using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class UsersDashViewModel
    {
        public string UserName { get; set; }
        public string Table { get; set; }
        public DateTime LastUpdated { get; set; }
        public TimeSpan Distance { get; set; }
        public int UpdatedTimes { get; set; }
        public long ForeignId { get; set; }    
    }
}