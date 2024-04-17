using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using FluentFTP;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Application;
using mes.Models.StatisticsModels;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Utilities;

namespace mes.Models.Services.Infrastructures
{
    public class StatisticsService
    {
        GeneralPurpose genP;
        const string intranetLog=@"c:\temp\intranet.log"; 
        public StatisticsService()
        {
            genP = new GeneralPurpose();
        }
        public List<DayStatistic> GetMachineStats(MachineDetails oneMachine, string startTime, string endTime)
        {
            List<DayStatistic> result = new List<DayStatistic>();
            string machineName = oneMachine.MachineName;
            string retrieveMethod = oneMachine.RetrieveMethod;
            string machineType = oneMachine.MachineType;
            string ftpTemp = oneMachine.FtpTempFolder;
            //string machineType = oneMachine.MachineType;
            //string retrieveMethod = config.AvailableMachines.Select(machine => machine.RetrieveMethod).FirstOrDefault();
            //string dataTypes = config.AvailableMachines.Select(machine => machine.DataTypes).FirstOrDefault();
            //string serverAddress = config.AvailableMachines.Select(machine => machine.ServerAddress).FirstOrDefault();
            //string userName = config.AvailableMachines.Select(machine => machine.UserName).FirstOrDefault();
            //string webGetString = config.AvailableMachines.Select(machine => machine.WebGetString).FirstOrDefault();

            //Biesse: P_yyyy_mm_dd.xsl (csv)
            //SCM1 e Busell: yyyymmdd.pro
            //Stefani: richiesta diretta via Web
            //Akron: richiesta web diretta
            //PRimus: RyyyyMMMdd.REP

            switch (machineType)
            {
                case "SCM2":
                    result = GetSCM2WebData(oneMachine, startTime, endTime);
                    break;

                case "BIESSE1":
                    result = GetBIESSE1Data(oneMachine, startTime, endTime, ftpTemp);
                    break;

                case "SCM1":
                    break;

                case "ftp":
                    break;
            }

            return result;
        }

    #region SCM2

    public List<SCM2ReportBody> GetSCM2WebRawData(MachineDetails oneMachine, string startTime, string endTime)
    {
        bool isALive = PingHost(oneMachine.ServerAddress);            

        DateTime startT = Convert.ToDateTime(startTime);
        DateTime endT = Convert.ToDateTime(endTime);

        string startPeriod = $"{startT.Year}-{startT.Month.ToString("00")}-{startT.Day.ToString("00")}T04%3A00%3A00";
        string endPeriod = $"{endT.Year}-{endT.Month.ToString("00")}-{endT.Day.ToString("00")}T21%3A00%3A00";

        string requestUrl = $"http://{oneMachine.ServerAddress}:{oneMachine.ServerPort}/api/v1/report/production?from={startPeriod}&to={endPeriod}";

        List<SCM2ReportBody> jsonReply = GetWebResponse(requestUrl);
        
        return jsonReply;
    }

    public List<DayStatistic> GetSCM2WebData(MachineDetails oneMachine, string startTime, string endTime)
    {
        bool isALive = PingHost(oneMachine.ServerAddress);            

        List<DayStatistic> result = new List<DayStatistic>();
        if(!isALive)
        {
            DayStatistic oneStat = new DayStatistic(){
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                ProgramsToday = 0,
                TimeOn = new TimeSpan(0,0,0),
                TimeWorking = new TimeSpan(0,0,0),                
                ProgramsPerHour = 0,
                IsAlive = false,
                ThicknessPieces = new List<KeyValuePair<int, double>>(){new KeyValuePair<int, double>(0,0)}
            };
            result.Add(oneStat);
            return result;           
        }

        DateTime startT = Convert.ToDateTime(startTime);
        DateTime endT = Convert.ToDateTime(endTime);

        string startPeriod = $"{startT.Year}-{startT.Month.ToString("00")}-{startT.Day.ToString("00")}T04%3A00%3A00";
        string endPeriod = $"{endT.Year}-{endT.Month.ToString("00")}-{endT.Day.ToString("00")}T21%3A00%3A00";

        string requestUrl = $"http://{oneMachine.ServerAddress}:{oneMachine.ServerPort}/api/v1/report/production?from={startPeriod}&to={endPeriod}";

        List<SCM2ReportBody> jsonReply = GetWebResponse(requestUrl);
        // TO DO : gestire errore di connessione o di risposta
        //----------------------------------------------
        //trasformazione dati
        //08/04/24
        //gestione responso spessori bordati per giorno
        
        List<DateTime> daysInInterval = jsonReply.Select(d => Convert.ToDateTime(d.DateTime).Date).Distinct().ToList();            

        foreach(DateTime oneDay in daysInInterval)
        {
            DateTime dayStart = jsonReply.Where(d => Convert.ToDateTime(d.DateTime).Date == oneDay)
                                        .Select(m => Convert.ToDateTime(m.DateTime))
                                        .Min();

            DateTime dayEnd = jsonReply.Where(d => Convert.ToDateTime(d.DateTime).Date == oneDay)
                                        .Select(m => Convert.ToDateTime(m.DateTime))
                                        .Max();
            
            int progs = jsonReply.Where(d => Convert.ToDateTime(d.DateTime).Date == oneDay).Count();            
            
            //08/04/2024
            //richiesta divisione per spessore
            List<KeyValuePair<int, double>> thicknessPieces = GetThicknessPieces(jsonReply, oneDay);
            
            TimeSpan timeWorking = new TimeSpan(0);

            //superfluo, sappiamo già che MachineType == "SCM2"
            if(oneMachine.MachineType == "SCM2") 
            {
                timeWorking = dayEnd - dayStart;
            }

            double prgPerHour = (progs / timeWorking.TotalMinutes)*60;

            double totalMetersConsumed = Math.Round(jsonReply.Where(w => Convert.ToDateTime(w.DateTime) >= dayStart)
                                            .Where(z => Convert.ToDateTime(z.DateTime) <= dayEnd)
                                            .Sum(t => Convert.ToDouble(t.EdgeConsumptionLH))/1000,2);

            double totalMeters = Math.Round(jsonReply.Where(w => Convert.ToDateTime(w.DateTime) >= dayStart)
                                            .Where(z => Convert.ToDateTime(z.DateTime) <= dayEnd)
                                            .Sum(t => Convert.ToDouble(t.Length))/1000,2);                                                

            result.Add(new DayStatistic(){
                StartTime = dayStart,
                EndTime = dayEnd,
                ProgramsToday = progs,
                TimeOn = dayEnd - dayStart,
                TimeWorking = timeWorking,
                ProgramsPerHour = Math.Round(prgPerHour,1),
                TotalMeters = totalMeters,
                TotalMetersConsumed = totalMetersConsumed,
                IsAlive = true,
                ThicknessPieces = thicknessPieces
            });                
        }

        return result;
    }

        public List<KeyValuePair<int, double>> GetThicknessPieces(List<SCM2ReportBody> jsonReply, DateTime queryDay)
        {
            List<KeyValuePair<int, double>> result = new List<KeyValuePair<int, double>>();
            // quanti spessori quel giorno
            // quanti pezzi per spessore

            var debug1 = Convert.ToDateTime(jsonReply[0].DateTime).Date;
            var debug2 = queryDay.Date;

            List<SCM2ReportBody> rawBody = jsonReply.Where(w => Convert.ToDateTime(w.DateTime).Date == queryDay.Date).ToList();

            List<double> thicknessesThatDay = rawBody.Select(t => t.Thickness).Distinct().ToList();

            foreach(double onethickness in thicknessesThatDay)                                                
            {
                int piecesThatThickThatDay = rawBody.Where(t => t.Thickness == onethickness).Count();
                
                KeyValuePair<int, double> oneResult = new KeyValuePair<int, double>(piecesThatThickThatDay, onethickness);
                
                result.Add(oneResult);
            }

            return result;
        }
        public List<SCM2ReportBody> GetWebResponse(string requestUrl)
        {
            List<SCM2ReportBody> results = new List<SCM2ReportBody>();
            var request = WebRequest.Create(requestUrl);

            HttpWebResponse response;

            string text = "";            

            try
            {
                response = (HttpWebResponse)request.GetResponse();
                if(response != null)
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        text = sr.ReadToEnd();                    
                    }
                }                
            }
            catch( Exception ex)
            {
                genP.Log2File($"StatisticService.GetWebResponse: {ex.Message}", intranetLog);
            }

            results = JsonConvert.DeserializeObject<List<SCM2ReportBody>>(StatusCutter(text.Replace("'","-")));

            return results;
        }

        private string StatusCutter(string jsonInput)
        {
            if(jsonInput == "") return "";            
            string middle = jsonInput.Substring(26, jsonInput.Length - 26 - 13);
            return middle;
        }
        public bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

    #endregion 

    #region BIESSE1
        public List<DayStatistic> GetBIESSE1Data(MachineDetails oneMachine, string startTime, string endTime, string ftpTemp)
        {
            List<DayStatistic> result = new List<DayStatistic>();
            GeneralPurpose genPurpose = new GeneralPurpose();

            //bool isALive = PingHost(oneMachine.ServerAddress); 
            
            FtpService ftp = new FtpService(oneMachine.ServerAddress, oneMachine.UserName, genPurpose.ImplicitPwd(oneMachine.UserName));
            //si collega
            //directory

            List<FtpListItem> remoteFilesItemRaw =  ftp.FtpDir($"/{oneMachine.MachineName}/");

            List<FtpListItem> remoteFilesItem =  ftp.FtpDir($"/{oneMachine.MachineName}/")
                                                    .Where(d => d.Modified >= Convert.ToDateTime(startTime))
                                                    .Where(d2 => d2.Modified <= Convert.ToDateTime(endTime))
                                                    .ToList();

            List<string> remoteFiles = remoteFilesItem.Select(x => x.Name).ToList();

            if(!Directory.Exists(oneMachine.FtpTempFolder)) Directory.CreateDirectory(oneMachine.FtpTempFolder);

            foreach(string oneFile in remoteFiles)
            {
                string localFile = Path.Combine(oneMachine.FtpTempFolder, oneFile);
                if(!File.Exists(localFile))
                {
                    ftp.FtpDownloadFile($"/{oneMachine.MachineName}/{oneFile}/", localFile);
                }
                List<BiesseReportModel> reportList = GetReportContent(localFile);
            }

            return result;
        }

        public List<BiesseReportModel> GetReportContent(string fileName)
        {
            List<BiesseReportModel> report = new List<BiesseReportModel>();
            List<string> rawFile = new List<string>(File.ReadAllLines(fileName));

            for(int i=1; i<rawFile.Count; i++)
            {
                string[] parts = rawFile[i].Split("\t");

                BiesseReportModel oneModel = new BiesseReportModel(){
                    StartDate = Convert.ToDateTime($"{parts[0]} {parts[2]}"),
                    EndDate = Convert.ToDateTime($"{parts[1]} {parts[3]}"),
                    StartTime = TimeSpan.Parse(parts[2]),
                    EndTime = TimeSpan.Parse(parts[3]),
                    Worklist = parts[4],
                    Program = parts[5],
                    Time = TimeSpan.Parse(parts[6]),
                    Origin = Convert.ToInt16(parts[7]),
                    Result = Convert.ToInt16(parts[8])                    
                };
                report.Add(oneModel);
            }
            return report;
        }

        public DayStatistic GetDayStatisticsFromBSReport(List<BiesseReportModel> inputList)
        {
            /// completa oggetto

            //TimeOn
            //TimeWorking
            //ProgramsPerHour
            DayStatistic oneDay = new DayStatistic()
            {
                StartTime = inputList.Min(d => d.StartDate),
                EndTime = inputList.Max(e=> e.EndDate),
                ProgramsToday = inputList.Count(),
                IsAlive = true    
            };

            return oneDay;
        }

    #endregion

        public void FormatMachineData(List<DayStatistic> inputStats,
                                        string machineType,
                                        out List<int>onTime,
                                        out List<int> workingTime,
                                        out List<string> daysNames,
                                        out List<int> progsPerDay,
                                        out List<double> progsPerHour,
                                        out List<double> totalMeters,
                                        out List<double> totalMetersConsumed)
        {
            onTime = new List<int>();
            workingTime = new List<int>();
            daysNames = new List<string>();
            progsPerDay = new List<int>();
            progsPerHour = new List<double>();
            totalMeters = new List<double>();
            totalMetersConsumed = new List<double>();
            
            foreach(DayStatistic oneStat in inputStats)
            {
                if(machineType =="SCM2")
                { onTime.Add(0);}
                else{ onTime.Add(Convert.ToInt32(oneStat.TimeOn.TotalMinutes));}
                
                workingTime.Add(Convert.ToInt32(oneStat.TimeWorking.TotalMinutes));
                daysNames.Add(oneStat.StartTime.ToString("dd MMM"));
                progsPerDay.Add(oneStat.ProgramsToday);
                progsPerHour.Add(Math.Round(oneStat.ProgramsPerHour,1));
                totalMetersConsumed.Add(Math.Round(oneStat.TotalMetersConsumed,2));
                totalMeters.Add(Math.Round(oneStat.TotalMeters,2));
            }
        }

        public List<HourStatistics> FormatMachineDailyData(MachineDetails oneMachine, string startTime, string endTime, out List<string> xLabels, out string title)
        {
            List<HourStatistics> result = new List<HourStatistics>();
            xLabels = new List<string>();

            List<SCM2ReportBody> dayData = GetSCM2WebRawData(oneMachine, startTime, endTime);

            List<int> hoursInInterval = dayData.Select(h => h.DateTime.Hour).Distinct().ToList();
            //____ composizione titolo pagina --------------------
            string oraInizio = dayData.Min(d => d.DateTime).ToString("HH:mm:ss");
            string oraFine = dayData.Max(d=> d.DateTime).ToString("HH:mm:ss");
            int latiTotali = dayData.Count();
            double metriTotali = Math.Round(dayData.Sum(m => m.Length)/1000,2);
            double metriConsumati = Math.Round(dayData.Sum(m=> m.EdgeConsumptionLH)/1000,2);

            title =$"orario di lavoro: {oraInizio}-{oraFine}, {latiTotali} lati, {metriTotali} metri bordati, {metriConsumati} bordo consumato";

            List<double> thicknesses = dayData.Select(t => t.Thickness).Distinct().ToList();
            //ottieni il massimo e il minimo di ogni ora nell'intervallo
            //----------------------------------------------------
            DateTime today = Convert.ToDateTime(startTime);

            foreach(double oneThick in thicknesses)
            {
                foreach(int oneHour in hoursInInterval)
                {
                    DateTime thisHourStart = new DateTime(today.Year, today.Month, today.Day, oneHour, 0, 0);
                    DateTime thisHourEnd = new DateTime(today.Year, today.Month, today.Day, oneHour, 59, 59);
                    List<SCM2ReportBody> oneHourData = dayData.Where(d => Convert.ToDateTime(d.DateTime)>= thisHourStart)
                                                                .Where(d2 => Convert.ToDateTime(d2.DateTime)<= thisHourEnd)
                                                                .Where(t => t.Thickness == oneThick).ToList();

                    List<SCM2ReportBody> oneHourDataLabel = dayData.Where(d => Convert.ToDateTime(d.DateTime)>= thisHourStart)
                                                                .Where(d2 => Convert.ToDateTime(d2.DateTime)<= thisHourEnd).ToList();                                                                
                    
                    string intervalMax = oneHourDataLabel.Max(d => d.DateTime).ToString("HH:mm");
                    string intervalMin = oneHourDataLabel.Min(d => d.DateTime).ToString("HH:mm");
                    string label2add = $"{intervalMin}-{intervalMax}";
                    if(!xLabels.Contains(label2add)) xLabels.Add(label2add);

                    double totalMeters = oneHourData.Sum(t => t.Length)/1000;
                    int totalSides = oneHourData.Count();
                    double consumedMeters = oneHourData.Sum(c => c.EdgeConsumptionLH)/1000;

                    HourStatistics oneHourStat = new HourStatistics(){
                        Hour = oneHour,
                        TotalMeters = totalMeters,
                        TotalSides = totalSides,
                        ConsumedMeters = consumedMeters,
                        Thickness = oneThick
                    };
                    result.Add(oneHourStat);
                }



            }        
            return result;
        }

        public string SeriesDataStringBuilder(List<HourStatistics> input)
        {
            string tempString = "";
            tempString += "[{";
            List<double> thicknesses = input.Select(t => t.Thickness).Distinct().ToList();
            foreach(var oneThick in thicknesses)
            {
                tempString +=$"name:'{oneThick} mm', data: [";
                List<int> thisThickSides = input.Where(t => t.Thickness == oneThick).Select(s => s.TotalSides).ToList();
                //string actualLast = tempString.Last();
                foreach(int sideN in thisThickSides)
                {                    
                    tempString += $"'{sideN}',";
                }
                tempString = tempString.Substring(0,tempString.Length-1);
                tempString += "]}, {";

            }            
            tempString = tempString.Substring(0,tempString.Length-3);
            tempString +="]";

            return tempString;
        }

    }
}