namespace mes.Models.ViewModels
{
    public class ProductionCalendarAssignment
    {
        public long id { get; set; }
        public string AssignedTo { get; set; }
        public string Enabled { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}