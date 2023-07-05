using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class MachineStatusPicker
    {
        //model per prelevare i dati da datasource.db/MachineStatus
        public long id { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string MachineName { get; set; }
        public string MachineState { get; set; }
        public string MachineType { get; set; }
        public string WorklistName { get; set; }
        public string ProgramName { get; set; }
        public string WorkTime { get; set; }
        public string TotalMeters { get; set; }
        public string TotalPanels { get; set; }
        public string Quantity { get; set; }
        public string Counter { get; set; }
        public string TotalQuantity { get; set; }
        public string TotalCounter { get; set; }
        public string EdgeName { get; set; }
        public string EdgeConsumption { get; set; }
        public string Thickness { get; set; }
        public string Passage { get; set; }
        public string TrackSpeed { get; set; }        
        public string CreatedOn { get; set; }
    }
}