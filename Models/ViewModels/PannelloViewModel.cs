namespace mes.Models.ViewModels
{
    public class PannelloViewModel
    {
        public long id { get; set; }
        public string Codice { get; set; }
        public string CodiceEsterno { get; set; }
        public string DataIngresso { get; set; }
        public string Nome { get; set; }
        public string Tipomateriale { get; set; }
        public string Colore { get; set; }
        public string Lunghezza { get; set; }
        public string Larghezza { get; set; }
        public string Spessore { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string Cliente { get; set; }
        public string Locazione { get; set; }
        public string Enabled { get; set; }
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}