namespace mes.Models.ViewModels
{
    public class DbModification
    {
        public string DbColumn { get; set; }
        public string PreviousValue { get; set; }
        public string NewValue { get; set; }
    }
}