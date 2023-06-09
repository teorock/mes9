namespace mes.Models.ViewModels
{
    public class ProductionCalendar
    {
        public long id { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string AssignedTo { get; set; }
        public string CompletionPerc { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string Enabled { get; set; }
        public string WeekNumber { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedOn { get; set; }
    }
}