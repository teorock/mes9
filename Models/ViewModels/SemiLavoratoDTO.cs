using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class SemiLavoratoDTO
    {
        public string Codice { get; set; }
        public string NomeArticolo { get; set; }
        public string Lunghezza { get; set; }
        public string Larghezza { get; set; }
        public string Spessore { get; set; }
        public string Diametro { get; set;}
        public string Colore { get; set; }
        public string Cliente { get; set; }
        public string TipoBordo { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }        
    }
}