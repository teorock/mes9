using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class BordoDTO
    {
        public string Codice { get; set; }
        public string Nome { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string Spessore { get; set; }
        public string Altezza { get; set; }
        public string Colore { get; set; }
        public string Fornitore { get; set; }    
    }
}