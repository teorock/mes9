using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using mes.Models.InfrastructureModels;

namespace mes.Models.Services.Infrastructures
{
    public class PfcServices
    {
        public List<PfcCsvDaneaSource> LoadCsvDaneaToList(string fileName, string filter1, string filter2)
        {
            List<PfcCsvDaneaSource> result = new List<PfcCsvDaneaSource>();

                List<string> rawFile = new List<string>(File.ReadLines(fileName));

                for (int i = 1; i < rawFile.Count; i++)
                {
                    string oneLine = rawFile[i];
                    string[] parts = oneLine.Split(';');
                    string stato = parts[4];

                    if (stato.Contains(filter1) || stato.Contains(filter2))
                    {
                        PfcCsvDaneaSource oneCsv = new PfcCsvDaneaSource()
                        {
                            Data = parts[1],
                            NCommessa = parts[2],
                            Cliente = parts[3],
                            Stato = parts[4],
                            DataConsegna = parts[5],
                            Commento = parts[6]
                        };
                        result.Add(oneCsv);
                    }
                }

            return result;
        }
    }
}