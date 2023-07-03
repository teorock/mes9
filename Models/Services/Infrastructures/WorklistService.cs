using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using mes.Models.ServicesConfigModels;
using mes.Models.ViewModels;
using Newtonsoft.Json;

namespace mes.Models.Services.Infrastructures
{
    public class WorklistService
    {
        const string worklistConfigFile = @"c:\core\mes\ServicesConfig\wlServiceConfig.json";

        WorklistServiceConfig config = new WorklistServiceConfig();

        public WorklistService()
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(worklistConfigFile))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<WorklistServiceConfig>(rawConf);                
        }

        public List<WorklistCounter> GetWorklistContent(string worklistFileName)
        {
            XDocument xdoc = XDocument.Load(worklistFileName);

            IEnumerable<XElement> rows = xdoc.Descendants();

            List<XElement> test = rows.Where(d => d.Name == config.WorkListCadElementLine).ToList();
            List<XElement> test2 = rows.Where(d => d.Name == config.WorkListCadElementLineA1).ToList();

            List<WorklistCounter> allListLines = new List<WorklistCounter>();

            foreach (XElement oneRow in test)
            {
                var programName = oneRow.Attributes().FirstOrDefault(c => c.Name == "Name").Value;
                int quantity = (int)oneRow.Attributes().FirstOrDefault(d => d.Name == "Quantity");
                int counter = (int)oneRow.Attributes().FirstOrDefault(e => e.Name == "Counter");
                var programUri = oneRow.Attributes().FirstOrDefault(c => c.Name == "ProgramUri").Value;

                WorklistCounter oneListCounter = new WorklistCounter()
                {
                    ProgramName = programName,
                    Quantity = quantity,
                    Counter = counter,
                    ProgramUri = programUri
                };
                allListLines.Add(oneListCounter);
            }

            foreach (XElement oneRow in test2)
            {
                var programName = oneRow.Attributes().FirstOrDefault(c => c.Name == "Name").Value;
                int quantity = (int)oneRow.Attributes().FirstOrDefault(d => d.Name == "Quantity");
                int counter = (int)oneRow.Attributes().FirstOrDefault(e => e.Name == "Counter");
                var programUri = oneRow.Attributes().FirstOrDefault(c => c.Name == "ProgramUri").Value;

                WorklistCounter oneListCounter = new WorklistCounter()
                {
                    ProgramName = programName,
                    Quantity = quantity,
                    Counter = counter,
                    ProgramUri = programUri
                };
                allListLines.Add(oneListCounter);
            }

            return allListLines;
        }        
    }
}