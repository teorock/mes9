using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class DipendenteViewModel
    {
	public long id { get; set; }
	public string Nome { get; set; }
	public string Cognome { get; set; }
	public string DataAssunzione { get; set; }
	public string Matricola { get; set; }
	public string Username { get; set; }
	public string Ruolo { get; set; }
	public string Enabled { get; set; }
	public string NotifyAddress { get; set; }
	public string CreatedOn { get; set; }
	public string CreatedBy { get; set; }        
    }
}