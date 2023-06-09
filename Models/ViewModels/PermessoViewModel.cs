using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class PermessoViewModel
    {
	public long id { get; set; }
	public string Nome { get; set; }
	public string Cognome { get; set; }
	public string DataInizio { get; set; }
	public string DataFine { get; set; }
	public string DataDiRichiesta { get; set; }
	public string Tipologia { get; set; }
	public string IntervalloTempo { get; set; }
	public string Motivazione { get; set; }
	public string Username { get; set; }
	public string Stato { get; set; }
	public string Urgente { get; set; }
	public string Note { get; set; }
	public string Enabled { get; set; }
	public string CreatedOn { get; set; }
	public string CreatedBy { get; set; }        
    }
}