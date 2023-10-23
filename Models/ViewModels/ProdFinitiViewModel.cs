using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class ProdFinitiViewModel
    {
        public long id { get; set; }
        public string Codice { get; set; }
        public string Descrizione { get; set; }
        public string Cliente { get; set; }
        public string Note { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string Enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}