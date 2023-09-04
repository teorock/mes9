using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class CollaDTO
    {
        public string Codice { get; set; }
        public string Nome { get; set; }
        public string FormatoColla { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string UnitaMisura { get; set; }    
    }
}