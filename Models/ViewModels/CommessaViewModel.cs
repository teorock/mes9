using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class CommessaViewModel
    {
        public string Cliente { get; set; }
        public string DataRicezione { get; set; }
        public string DataConsegnaRichiesta { get; set; }
        public string DataConsegnaEffettiva { get; set; }
        public string DataInizioLav { get; set; }
        public string DataFineLav { get; set; }
        public string TipoLavorazioni { get; set; }
        public string Commento { get; set; }
        public string Note { get; set; }
        public string DaEtichettare { get; set; }
        public string FileEtichette { get; set; }
        public string EtichetteNostre { get; set; }
        public string CodCommessaInterno { get; set; }
        public string CodCommessaEsterno { get; set; }
        public string Materiale { get; set; }
        public string Colore { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}