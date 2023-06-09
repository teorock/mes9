using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class LavorazioniViewModel
    {
        public string NomeLavorazione { get; set; }
        public string CodiceLavorazione { get; set; }        
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }        
    }
}