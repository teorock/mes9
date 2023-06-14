using System;
using System.Collections.Generic;
using System.IO;

namespace mes.Models.Services.Infrastructures
{
    public class GeneralPurpose
    {
        public void WriteList2Disk(List<string>inputList, string outputPath)
        {
            using(StreamWriter sw = new StreamWriter(outputPath))
            {
                foreach(string oneLine in inputList)
                {
                    sw.WriteLine(oneLine);
                }
            }
        }
        public void WriteFile2Disk(string inputString, string outputPath)
        {
            using(StreamWriter sw = new StreamWriter(outputPath))
            {
                sw.WriteLine(inputString);
            }
        }

        public void BackupCalendarFile(string destFile)
        {
            if(!File.Exists(destFile)) return;
            string bckName = Path.Combine(
                                        Path.GetDirectoryName(destFile),
                                        DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + Path.GetFileName(destFile));
            File.Copy(destFile, bckName, true);
        }

        public string PermissionTimeSpan(string startTime, string endTime, string tipologia)
        {
            //if tipologia= ferie, non calcola le ore
            string result="";
            string minutes="";
            string comment="";

            TimeSpan duration = DateTime.Parse(endTime).Subtract(DateTime.Parse(startTime));

            decimal giorni = Math.Round(Convert.ToDecimal(duration.TotalDays), 0);            

            if(tipologia=="ferie" | tipologia=="malattia")
                return $"{giorni+1}g";
            
            decimal ore = Math.Round(Convert.ToDecimal(duration.TotalHours), 0);
            bool arrotondato = false;
            if(duration.Minutes > 0 & duration.Minutes <=29)
            {
                minutes = "30";
                arrotondato = true;
            }
            if(duration.Minutes > 31 & duration.Minutes <= 59)
            {
                minutes = "";
                ore +=1;
                arrotondato = true;
            }            
            if(duration.Minutes == 30) minutes="30";
            //pausa pranzo
            if ((DateTime.Parse(startTime).Hour < 12 & DateTime.Parse(endTime).Hour >12)|
                    (DateTime.Parse(startTime).Hour <13 & DateTime.Parse(endTime).Hour>13))
                    {
                        ore -= 1;
                        comment +="-1h pranzo";
                    }

            comment = $"[{duration.Hours}h{duration.Minutes}]";

            result = (arrotondato)? $"{ore}h{minutes} {comment}":$"{ore}h{minutes}";


            return result;
        }

        public DateTime PermessiDate2Calendar(string inputString)
        {
            int year = Convert.ToInt32(inputString.Substring(0,4));
            int month = Convert.ToInt32(inputString.Substring(5,2));
            int day = Convert.ToInt32(inputString.Substring(8,2));
            int hour = Convert.ToInt32(inputString.Substring(11,2));
            int minute = Convert.ToInt32(inputString.Substring(14,2));
            
            return new DateTime(year,month,day,hour,minute,0);
        }

        public DateTime String2DateConverter (string input)
        {
            DateTime result = DateTime.Parse(input);

            string debug = result.ToString();

            return result;
        }
        public T DenullifyObj<T>(T obj)
        {
            var properties = typeof(T).GetProperties();

            object result = (T)Activator.CreateInstance(typeof(T));

            foreach (var property in properties)
            {
                var test = property.GetValue(obj);
                if (property.GetValue(obj) == null)
                {
                    if(property.PropertyType==typeof(string)) property.SetValue(result, "---");
                }
                else
                {
                    property.SetValue(result, property.GetValue(obj));
                }
            }

            return (T)result;
        }        
    }
}