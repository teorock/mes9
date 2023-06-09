namespace mes.Models.ViewModels
{
    public class CollaViewModel
    {
        public long id { get; set; }
        public string Codice { get; set; }
        public string Nome { get; set; }
        public string FormatoColla { get; set; }
        public string Quantita { get; set; }
        public string QuantitaMin { get; set; }
        public string UnitaMisura { get; set; }
        public string Enabled { get; set; }   
        public string CreatedOn { get; set; }
        public string CreatedBy { get; set; }
    }
}