namespace mes.Models.ViewModels
{
    public class ProductionRequest
    {
        public long id { get; set; }
        public string Cliente { get; set; }
        public string Articolo { get; set; }        
        public string Descrizione { get; set; }
        public string DataCons { get; set; }
        public string Richiesti { get; set; }
        public string Disponibili { get; set; }
        public string CodSemilavorato { get; set; }
        public string DispSemilav { get; set; }
        public string CodPannello { get; set; }
        public string DispPannello { get; set; }
        public string Finitibg { get; set; } //colore di sfondo
        public string Semilavbg { get; set; } //colore di sfondo
        public string Pannellibg { get; set; } //colore di sfondo
        public string Enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}