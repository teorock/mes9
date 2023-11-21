using System.Collections.Generic;

namespace mes.Models.ControllersConfigModels
{
    public class ProductionControllerConfig
    {
        public string ConnString { get; set; }
        public string DbTable { get; set; }
        public List<string> UsersDashTables { get; set; }
        public string DbMovementsConnString { get; set; }
        public string DbMovementsTable { get; set; }
        public List<string> DbMovementsDbFilter { get; set; }        
        public string CustomerListConnString { get; set; }
        public string CustomersDbTable { get; set; }
        public string ArticlesListConnString { get; set; }
        public string ArticlesDbTable { get; set; }
        

    }
}