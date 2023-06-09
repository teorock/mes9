using System.Collections.Generic;

namespace mes.Models.ViewModels
{
    public class Task
    {
        public string id { get; set; }
            public string name { get; set; }
            public int progress { get; set; }
            public bool progressByWorklog { get; set; }
            public string description { get; set; }
            public string code { get; set; }
            public int level { get; set; }
            public string status { get; set; }
            public string depends { get; set; }
            public double start { get; set; }
            public double duration { get; set; }
            public double end { get; set; }
            public bool startIsMilestone { get; set; }
            public bool endIsMilestone { get; set; }
            public bool collapsed { get; set; }
            public bool canWrite { get; set; }
            public bool canAdd { get; set; }
            public bool canDelete { get; set; }
            public bool canAddIssue { get; set; }
            public List<string> assigs { get; set; }    
    }
}