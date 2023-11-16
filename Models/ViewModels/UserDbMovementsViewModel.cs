using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class UserDbMovementsViewModel
    {
        public string User { get; set; }
        public DateTime LastMod { get; set; }
        public int TotalMods { get; set; }
        public string LastModDetails { get; set; }
    }
}