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

        public string PermissionTimeSpan(string startTime, string endTime)
        {

            TimeSpan duration = DateTime.Parse(endTime).Subtract(DateTime.Parse(startTime));

            decimal giorni = Math.Round(Convert.ToDecimal(duration.TotalDays), 0) ;
            
            string result ="";
            //string days= (giorni ==0) ? "" : $"{giorni} giorni e ";
            string days= "g";
            string hours ="";

            if (giorni==0)
            {
                //if(duration.Minutes == 0) hours = $"{duration.Hours}h";
                if(duration.Minutes <= 30) hours = $"{duration.Hours}h";
                if(duration.Minutes >= 30) hours = $"{duration.Hours + 1}h [{duration.Hours}h{duration.Minutes}m]";
            }

            result = days + hours;

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