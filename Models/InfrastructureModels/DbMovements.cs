namespace mes.Models.ViewModels
{
    public class DbMovements
    {
        public long id { get; set; }
        public string DbName { get; set; }
        public string DbTable { get; set; }
        public string DbColumn { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string OperationType { get; set; }        
        public string PreviuosVal { get; set; }
        public string NewVal { get; set; }
        public string User { get; set; }        
        public string ModifiedOn { get; set; }
    }
}