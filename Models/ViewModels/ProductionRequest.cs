namespace mes.Models.ViewModels
{
    public class ProductionRequest
    {
        public long id { get; set; }
        public string Cliente { get; set; }
        public string Modello { get; set; }
        public string DataCons { get; set; }
        public string Diametro { get; set; }
        public string Spessore { get; set; }
        public string Richiesti { get; set; }
        public string Disponibili { get; set; }
        public string CodSemilavorati { get; set; }
        public string Lastre { get; set; }
        public string Enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}