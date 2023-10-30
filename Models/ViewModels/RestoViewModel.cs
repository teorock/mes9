using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class RestoViewModel
    {
        public long id { get; set; }
        public string Codice { get; set; }
        public string Materiale { get; set; }
        public string Colore { get; set; }
        public string Lunghezza { get; set; }
        public string Larghezza { get; set; }
        public string Spessore { get; set; }
        public string Quantita { get; set; }
        public string Cliente { get; set; }
        public string Enabled { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }        
    }
}