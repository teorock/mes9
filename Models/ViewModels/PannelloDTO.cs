using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class PannelloDTO
    {
        public string Codice { get; set; }
        public string CodiceEsterno { get; set; }
        public string DataIngresso { get; set; }
        public string Nome { get; set; }
        public string Tipomateriale { get; set; }
        public string Colore { get; set; }
        public string Lunghezza { get; set; }
        public string Larghezza { get; set; }
        public string Spessore { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string Cliente { get; set; }
        public string Locazione { get; set; }    
    }
}