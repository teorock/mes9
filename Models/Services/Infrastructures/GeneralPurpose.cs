using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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

            DateTime endDatetime = String2DateTime(endTime);
            DateTime startDatetime = String2DateTime(startTime);

            TimeSpan duration = endDatetime.Subtract(startDatetime);

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
            if ((startDatetime.Hour < 12 & endDatetime.Hour >12)|
                    (startDatetime.Hour <13 & endDatetime.Hour>13))
                    {
                        ore -= 1;
                        comment +="-1h pranzo";
                    }

            comment = $"[{duration.Hours}h{duration.Minutes}]";

            result = (arrotondato)? $"{ore}h{minutes} {comment}":$"{ore}h{minutes}";


            return result;
        }

        private DateTime String2DateTime(string input)
        {
            //funzione deprecata
            //non più necessaria in quanto è stato effettuato il debug
            
            DateTime result = DateTime.Now;

            if(!input.Contains('T'))
            {
                int year = Convert.ToInt16(input.Substring(6,4));
                int month = Convert.ToInt16(input.Substring(3,2));
                int day = Convert.ToInt16(input.Substring(0,2));
                int hour = Convert.ToInt16(input.Substring(11,2));
                int minute = Convert.ToInt16(input.Substring(14,2));

                result = new DateTime(year,month,day,hour, minute, 0);
            }
            if(input.Contains('T'))
            {
                result = DateTime.Parse(input)               ;
            }
            

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

        public string ImplicitPwd(string inputString)
        {
            string res = "";

            foreach (char c in inputString)
            {
                switch (c)
                {
                    case 'a':
                        res += '4';
                        break;
                    case 'A':
                        res += '4';
                        break;
                    case 'i':
                        res += '1';
                        break;
                    case 'I':
                        res += '1';
                        break;
                    case 'e':
                        res += '3';
                        break;
                    case 'E':
                        res += '3';
                        break;
                    case 'o':
                        res += '0';
                        break;
                    case 'O':
                        res += '0';
                        break;
                    case 's':
                        res += '$';
                        break;
                    case 'S':
                        res += '$';
                        break;
                    default:
                        res += c;
                        break;
                }
            }
            return res;
        }


        public List<string> ExportObj2CsvList<T>(List<T> genericList)
        {
            List<string> result = new List<string>();

            var header = "";
            var info = typeof(T).GetProperties();

            foreach (var prop in typeof(T).GetProperties())
            {
                header += prop.Name + "; ";
            }
            header = header.Substring(0, header.Length - 2);
            result.Add(header);

            foreach (var obj in genericList)
            {
                var line = "";
                foreach (var prop in info)
                {
                    line += prop.GetValue(obj, null) + "; ";
                }                    
                line = line.Replace(',','.').Substring(0, line.Length - 2);
                result.Add(line); 
            }

            return result;
        }

        public string List2Csv(List<string> inputString)
        {
            var csvContent = new StringBuilder();
            foreach (var item in inputString)
            {
                csvContent.AppendLine(item);
            }

            return csvContent.ToString(); 
        }

        public DateTime GetWeeksMonday(int addDays)
        {
            int daysUntilSunday = (int)DateTime.Now.DayOfWeek - (int)DayOfWeek.Sunday - 1;
            DateTime firstDayOfThisWeek = DateTime.Now.AddDays(-daysUntilSunday);
            return firstDayOfThisWeek.AddDays(addDays);
        }

        public List<DateTime> GetWeekDaysInterval (DateTime startDate, int startDay, int endDay)
        {
            List<DateTime> result = new List<DateTime>();
            for(int day=startDay; day<endDay; day++)
            {
                result.Add(GetWeeksMonday(day));
            }

            return result;
        }

        public void Log2File(string line2log, string filePath)
        {
            using(StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine($"{DateTime.Now} -> {line2log}");
            }
        }  

    }
}