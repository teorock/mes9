using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class LavorazioneViewModel
    {
        public long id { get; set; }
        public string NomeLavorazione { get; set; }
        public string CodiceLavorazione { get; set; }
        public string Enabled { get; set; }   
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }        
    }
}