using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class PermessoFilter
    {
        public string Username { get; set; }
        public string DataInizio { get; set; }
        public string DataFine { get; set; }
        public string Tipologia { get; set; }
    }
}