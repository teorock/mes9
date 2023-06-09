using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class MaterialiViewModel
    {
        public string Nome { get; set; }
        public string Colore { get; set; }
        public string CodiceInterno { get; set; }
        public string CodiceEsterno { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}