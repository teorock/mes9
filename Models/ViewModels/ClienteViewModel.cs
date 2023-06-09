using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class ClienteViewModel
    {
        public long id { get; set; }
        public string Nome { get; set; }
        public string CodiceEsterno { get; set; }
        public string Indirizzo { get; set; }
        public string Telefono { get; set; }
        public string Referente1 { get; set; }
        public string TelReferente1 { get; set; }
        public string Referente2 { get; set; }
        public string TelReferente2 { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}