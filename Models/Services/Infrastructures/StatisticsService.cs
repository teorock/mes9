using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using FluentFTP;
using FluentFTP.Helpers;
using iTextSharp.text;
using mes.Models.ControllersConfigModels;
using mes.Models.Services.Application;
using mes.Models.StatisticsModels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using ServiceStack.Text;

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
            //Akron aggiornamento(20giu2024): attenzione, i metri lineari totali e il numero totale di pannelli non sono legati ad un fattore temporale
                //la richiesta 'http://192.168.2.55:8082/TOTALE_METRI_LINEARI' produce solo { "value": 122457.710067}
                //quindi, per contestualizzare nel tempo è necessario prelevare ad intervalli e registrare il dato su db (come già realizzato)
                //in fine, per avere un andamento temporale della Akron è solo possibile interrogare il db e NON la macchina direttamente
                //ATTENZIONE: per il conteggio dei pannelli fatti in un giorno usare TOTALPANEL. Le righe con descrizione "PANEL ENTERED" servono solo per il conteggio degli spessori lavorati quel giorno
            //PRimus: RyyyyMMMdd.REP

            switch (machineType)
            {
                case "SCM2":
                    result = GetSCM2WebData(oneMachine, startTime, endTime);
                    break;

                case "BIESSE1":
                    result = GetBIESSE1Data(oneMachine, startTime, endTime, ftpTemp);
                    break;

                case "BIESSE2": //Akron
                    result = GetBIESSE2Data(oneMachine.DbDataSource, oneMachine.DbTable, startTime, endTime);
                    break;

                case "SCM1":
                    result = GetSCM1ReportData(oneMachine, startTime, endTime);
                    break;

                case "ftp":
                    break;
                
                case "POMPA1":
                    result = GetPOMPA1Data(oneMachine);
                    break;
            }

            return result;
        }

    #region POMPA1

        public List<DayStatistic> GetPOMPA1Data(MachineDetails oneMachine)    
        {
            string webRequest = $"http://{oneMachine.ServerAddress}{oneMachine.WebGetString}";

            List<AtlasCopcoData> rawDatas = GetAtlasCopcoWebResponse(webRequest);

            return new List<DayStatistic>();
        }

        public List<AtlasCopcoData> GetAtlasCopcoWebResponse(string webRequest)
        {
            List<AtlasCopcoData> result = new List<AtlasCopcoData>();
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            // Ensure TLS 1.2 is used
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = WebRequest.Create(webRequest);

            request.ContentType = "application/json; charset=utf-8";

            HttpWebResponse response;

            string text;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception excp)
            {
                //Logger(excp.Message);
                return result;
            }

            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
            }

            result = JsonConvert.DeserializeObject<List<AtlasCopcoData>>(text);
            return result;
        }

    #endregion

    #region BIESSE2

    public List<DayStatistic> GetBIESSE2Data(string dbDataSource, string dbTable, string startTime, string endTime)
    {
        List<DayStatistic> result = new List<DayStatistic>();
            
        DatabaseAccessor db = new DatabaseAccessor();
        List<AkronDataRaw> rawData = db.Queryer<AkronDataRaw>(dbDataSource, dbTable);
        List<AkronData> data = AkronDataRaw2AkronDataMapper(rawData, startTime, endTime);
        result = AkronData2DayStatisticMapper(data);


        return result;
    }

    public List<AkronData> AkronDataRaw2AkronDataMapper(List<AkronDataRaw> rawData, string startDate, string endDate)
    {        
        List<AkronData> result = new List<AkronData>();
        DateTime startPeriod = Convert.ToDateTime(startDate).AddHours(-1);
        DateTime endPeriod = Convert.ToDateTime(endDate).AddDays(1).AddHours(-1);

        foreach(AkronDataRaw oneRaw in rawData)
        {
            DateTime date = Convert.ToDateTime(oneRaw.Date);

            var debug = oneRaw;

            double totalMeters = (oneRaw.TotalMeters == "")? 0: Math.Round(Convert.ToDouble(oneRaw.TotalMeters, CultureInfo.InvariantCulture), 2);

            AkronData oneData = new AkronData()                
            {
                Date = date,
                Code = Convert.ToInt32(oneRaw.Code),
                Descr = oneRaw.Descr,
                A = oneRaw.A, 
                B = oneRaw.B,
                C= oneRaw.C,
                D = oneRaw.D,
                E = oneRaw.E,
                F = oneRaw.F,
                G = oneRaw.G,
                TotalMeters = totalMeters,
                TotalPanels = Int32.TryParse(oneRaw.TotalPanels, out int panels) ? panels : 0
            };
            result.Add(oneData);
        }

        //var beginPer = GiveMeSomeTime(startDate, 0);
        //var endPer = GiveMeSomeTime(endDate, 1);

        List<AkronData> restrictedResult = result.Where(d => d.Date >= startPeriod).Where(d2 => d2.Date <= endPeriod).ToList();        

        return restrictedResult;        
    }

    private DateTime GiveMeSomeTime(string dateTime, int addDays)
    {
        // sono costretto a verificare il formato della data perchè arrivano in due formati diversi (forse dalla pagina statistiche del singolo giorno)
        int year = 2000;
        int month = 1;
        int day = 1;

        if(dateTime[2]=='/')
        {
            day = Convert.ToInt16(dateTime.Substring(0,2));
            month = Convert.ToInt16(dateTime.Substring(3,2));
            year = Convert.ToInt16(dateTime.Substring(6,4));
        }

        if(dateTime[4] == '-')
        {
            year = Convert.ToInt16(dateTime.Substring(0,4));
            month = Convert.ToInt16(dateTime.Substring(5,2));
            day = Convert.ToInt16(dateTime.Substring(8,2));
        }


        DateTime result = new DateTime(year, month, day, 0,0,0);
        return result.AddDays(addDays);

    }

    public List<DayStatistic> AkronData2DayStatisticMapper(List<AkronData> data)
    {
        List<DayStatistic> result = new List<DayStatistic>();

        //------------------
            List<DateTime> dates = data.Select(x => x.Date.Date).Distinct().ToList();

            foreach (DateTime oneDay in dates)
            {
                TimeSpan beginTime = data.Where(d => d.Date.Date == oneDay).Select(t => t.Date.TimeOfDay).Min();
                TimeSpan endTime = data.Where(d => d.Date.Date == oneDay).Select(t => t.Date.TimeOfDay).Max();                            

                DateTime todayStart = new DateTime(oneDay.Year, oneDay.Month, oneDay.Day, beginTime.Hours, beginTime.Minutes, beginTime.Seconds);
                DateTime todayEnd = new DateTime(oneDay.Year, oneDay.Month, oneDay.Day, endTime.Hours, endTime.Minutes, endTime.Seconds);
                
                TimeSpan totalTimeToday = todayEnd - todayStart;

                double todayStartMeters = data.Where(d => d.Date == todayStart).Select(m => m.TotalMeters).FirstOrDefault();
                double todayEndMeters = data.Where(d => d.Date == todayEnd).Select(m => m.TotalMeters).FirstOrDefault();

                double todayTotalMeters = Math.Round(todayEndMeters - todayStartMeters,2);


                int todayStartPanels = data.Where(d => d.Date == todayStart).Select(m => m.TotalPanels).FirstOrDefault();
                int todayEndPanels = data.Where(d => d.Date == todayEnd).Select(m => m.TotalPanels).FirstOrDefault();

                int todayTotalPanels = todayEndPanels - todayStartPanels;


                double progsPerHour = (todayTotalPanels!=0) ? Math.Round(todayTotalPanels / totalTimeToday.TotalHours,2) : 0;

                List<string> spessori = data.Where(d => d.Date.Date == oneDay)
                                    .Where(e => e.Descr == "PANEL ENTERED")
                                    .Select(s => s.E)
                                    .Distinct().ToList();

                List<KeyValuePair<int,double>> thicknessPieces = new List<KeyValuePair<int, double>>();
                foreach(string oneThick in spessori)
                {
                    //quanti sono per questo spessore
                    int panels4thick = data.Where(d => d.Date.Date == oneDay)
                                            .Where(e => e.Descr == "PANEL ENTERED")
                                            .Where(s => s.E == oneThick)
                                            .Count();
                    KeyValuePair<int,double> onePair = new KeyValuePair<int, double>(panels4thick, Convert.ToDouble(oneThick));
                    thicknessPieces.Add(onePair);
                }

                //mappo su DayStatistic e aggiungo alla lista
                DayStatistic oneStat = new DayStatistic()
                {
                    StartTime = todayStart,
                    EndTime = todayEnd,
                    ProgramsToday = todayTotalPanels,
                    TimeOn = totalTimeToday,
                    TimeWorking = totalTimeToday,
                    ThicknessPieces = thicknessPieces,
                    ProgramsPerHour = progsPerHour,
                    TotalMeters = todayTotalMeters,
                    TotalMetersConsumed = 0,
                    IsAlive = true,
                    TimePowerOn = todayStart,
                    TimePowerOff = todayEnd
                };
                result.Add(oneStat);
            }

        return result;
    }

    #endregion

    #region SCM2
    public List<SCM2ReportBody> GetSCM2WebRawData(MachineDetails oneMachine, string startTime, string endTime)
    {
        bool isALive = PingHost(oneMachine.ServerAddress);            
        List<SCM2ReportBody> jsonReply = new List<SCM2ReportBody>();

        if(isALive)
        {
            DateTime startT = Convert.ToDateTime(startTime);
            DateTime endT = Convert.ToDateTime(endTime);

            string startPeriod = $"{startT.Year}-{startT.Month.ToString("00")}-{startT.Day.ToString("00")}T04%3A00%3A00";
            string endPeriod = $"{endT.Year}-{endT.Month.ToString("00")}-{endT.Day.ToString("00")}T21%3A00%3A00";

            string requestUrl = $"http://{oneMachine.ServerAddress}:{oneMachine.ServerPort}/api/v1/report/production?from={startPeriod}&to={endPeriod}";

            jsonReply = GetWebResponse(requestUrl);
        }        
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
            bool debug = false;    
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

            if(debug)
            {
                using(StreamWriter sw = new StreamWriter(@"c:\temp\scm2_response.json"))
                {                
                    sw.WriteLine(text);
                }
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
            
            FtpService ftp = new FtpService(oneMachine.ServerAddress, oneMachine.UserName, genPurpose.ImplicitPwd(oneMachine.UserName));        
            List<FtpListItem> ftpDir = ftp.FtpDir($"/{oneMachine.MachineName}/");
            List<string> ftpNames = ftpDir.Select(n => n.Name).ToList();

            List<string> remoteFiles = BSReportFilesList(Convert.ToDateTime(startTime), Convert.ToDateTime(endTime));

            if(!Directory.Exists(oneMachine.FtpTempFolder)) Directory.CreateDirectory(oneMachine.FtpTempFolder);

            foreach(string oneFile in remoteFiles)
            {
                if(ftpNames.Contains(oneFile))
                {
                    string localFile = Path.Combine(oneMachine.FtpTempFolder, oneFile);
                    ftp.FtpDownloadFile($"/{oneMachine.MachineName}/{oneFile}/", localFile);
                    List<BiesseReportModel> reportList = GetReportContent(localFile);
                    DayStatistic oneDay = GetDayStatisticsFromBSReport(reportList);
                    result.Add(oneDay);
                }
            }

            return result;
        }

        private List<string> BSReportFilesList(DateTime startDate, DateTime endDate)
        {
            List<string> result = new List<string>();

            double totalDays = Math.Round((endDate-startDate).TotalDays,0) +1;

            for(int i=0; i<totalDays; i++)
            {
                DateTime oneDate = startDate.AddDays(i);
                string year = oneDate.Year.ToString();
                string month = oneDate.Month.ToString("00");
                string day = oneDate.Day.ToString("00");
                string file2add = $"P_{year}_{oneDate.Month.ToString("00")}_{oneDate.Day.ToString("00")}.xls";
                result.Add(file2add);
            }

            return result;
        }

        public List<BiesseReportModel> GetReportContent(string fileName)
        {
            string onlyName = Path.GetFileNameWithoutExtension(fileName);
            string thisFileDate= onlyName.Substring(2, onlyName.Length-2);
            List<BiesseReportModel> report = new List<BiesseReportModel>();
            List<string> rawFile = new List<string>(File.ReadAllLines(fileName));

            //refactor del BiesseReportModel
            //aggiungere proprietà TotalPieces per conteggio totale dei pezzi fatti con quella riga di programma
            //proprietà booleana IsNesting
            //proprietà int NestingPieces

            //successivo refactor 
            //al momento il numero di pezzi del giorno è prodotto contando il numero di elementi nella lista
            //dovrà invece essere prodotto dal conteggio

            for(int i=1; i<rawFile.Count; i++)
            {
                string[] parts = rawFile[i].Split("\t");

            int pezziTotali = 0;
            bool tempHasNesting = false;
        
                if(parts[5].Contains("PZ_"))
                {
                    List<string> elements = parts[5].Split("_").ToList();
                    string piecesPart = elements.Where(p => p.Contains("PZ")).FirstOrDefault();
                    pezziTotali = Convert.ToInt32(piecesPart.Substring(0, piecesPart.IndexOf('P')));
                    tempHasNesting = true;
                }

                BiesseReportModel oneModel = new BiesseReportModel(){
                    StartDate = Convert.ToDateTime($"{thisFileDate.Replace('_','-')} {parts[2]}"),
                    EndDate = Convert.ToDateTime($"{thisFileDate.Replace('_','-')} {parts[3]}"),
                    StartTime = TimeSpan.Parse(parts[2]),
                    EndTime = TimeSpan.Parse(parts[3]),
                    Worklist = parts[4],
                    Program = parts[5],
                    Time = TimeSpan.Parse(parts[6]),
                    Origin = Convert.ToInt16(parts[7]),
                    Result = Convert.ToInt16(parts[8]),
                    TotalPieces = pezziTotali,
                    HasNesting = tempHasNesting,
                    ReportLines = rawFile.Count                
                };
                report.Add(oneModel);
            }
            return report;
        }

        public DayStatistic GetDayStatisticsFromBSReport(List<BiesseReportModel> inputList)
        {
            DateTime startTime = inputList.Min(d => d.StartDate);
            DateTime endTime = inputList.Max(e=> e.EndDate);
            TimeSpan totaleOre = endTime.TimeOfDay - startTime.TimeOfDay;
       
            double progPerOra = Math.Round((inputList.Count()/totaleOre.TotalMinutes)*60,2);            
            var totaleTempoProgrammi = new TimeSpan(inputList.Sum(r => r.Time.Ticks));
            List<TimeSpan> test = inputList.Select(t => t.Time).ToList();

            DayStatistic oneDay = new DayStatistic()
            {
                StartTime = startTime,
                EndTime = endTime,
                //ProgramsToday = inputList.Count(),
                ProgramsToday = GetBIESSE1ReportListTotalPieces(inputList),
                ProgramsPerHour = progPerOra,
                TimeOn = totaleOre - totaleTempoProgrammi,
                TimeWorking = totaleTempoProgrammi,
                ThicknessPieces = new List<KeyValuePair<int, double>>(){new KeyValuePair<int, double>(1,1)},
                TotalMeters = 1,
                TotalMetersConsumed = 1,
                IsAlive = true    
            };
            return oneDay;
        }

    #endregion

#region SCM1

    public List<DayStatistic> GetSCM1ReportData(MachineDetails oneMachine, string startTime, string endTime)
    {
        //get raw data
        List<SCM1ReportModel> report = GetSCM1RawData(oneMachine, startTime, endTime);
        // map data
        List<DayStatistic> result = SCM1Mapper(report);

        return result; 
    }

    private List<SCM1ReportModel> GetSCM1RawData(MachineDetails oneMachine, string startTime, string endTime)
    {
        //ftp lentissimo
        //spostare ftp su dc2

        List<SCM1ReportModel> result = new List<SCM1ReportModel>();
        GeneralPurpose genPurpose = new GeneralPurpose();
        
        FtpService ftp = new FtpService(oneMachine.ServerAddress, oneMachine.UserName, genPurpose.ImplicitPwd(oneMachine.UserName));        
        List<FtpListItem> ftpDir = ftp.FtpDir($"/{oneMachine.MachineName}/");
        List<string> ftpNames = ftpDir.Select(n => n.Name).ToList();

        List<string> periodFileNames = SCM1FilenameGenerator(startTime, endTime);

        foreach(string oneFilename in periodFileNames)
        {
            if (ftpNames.Contains(oneFilename))
            {
                string localFile = Path.Combine(oneMachine.FtpTempFolder, oneFilename);
                ftp.FtpDownloadFile($"/{oneMachine.MachineName}/{oneFilename}/", localFile);
                result.AddRange(GetSCM1DataFromFile(localFile));
            }
        }
        //togli i nomi file con '_previous.pro'
        //da startTime e endTime compila la lista di tutti i nomi file da scaricare, secondo la convenzione nomiFile della scm (yyyyMMdd.pro)
        //per ogni nome file in questa lista
        //scaricalo
        //analizzalo

        return result;
    }

    private List<SCM1ReportModel> GetSCM1DataFromFile(string localFile)
    {
        List<SCM1ReportModel> report = new List<SCM1ReportModel>();
        List<string> rawFile = new List<string>(File.ReadAllLines(localFile));

        for (int i = 1; i < rawFile.Count(); i++)
        {
            string[] parts = rawFile[i].Split(',');

            TimeSpan start = new TimeSpan(Convert.ToInt16(parts[6]), Convert.ToInt16(parts[7]), Convert.ToInt16(parts[8]));
            TimeSpan stop = new TimeSpan(Convert.ToInt16(parts[9]), Convert.ToInt16(parts[10]), Convert.ToInt16(parts[11]));
            TimeSpan tEffettivo = new TimeSpan(Convert.ToInt16(parts[12]), Convert.ToInt16(parts[13]), Convert.ToInt16(parts[14]));
            TimeSpan tTotale = new TimeSpan(Convert.ToInt16(parts[15]), Convert.ToInt16(parts[16]), Convert.ToInt16(parts[17]));
            int quantita = Convert.ToInt16(parts[18]);
            TimeSpan tMedio = new TimeSpan(Convert.ToInt16(parts[19]), Convert.ToInt16(parts[20]), Convert.ToInt16(parts[21]), Convert.ToInt16(parts[22]));

            DateTime referenceDay = GetDatetimeFromFilename(Path.GetFileNameWithoutExtension(localFile));

            SCM1ReportModel oneModel = new SCM1ReportModel()
            {
                ReferenceDay = referenceDay, 
                Area = parts[0],
                FilePGM = Path.GetFileNameWithoutExtension(parts[1]),
                DimX = Convert.ToDouble(parts[3]),
                DimY = Convert.ToDouble(parts[4]),
                DimZ = Convert.ToDouble(parts[5]),
                Start = start,
                Stop = stop,
                Teffettivo = tEffettivo,
                Ttotale = tTotale,
                Quantita = quantita,
                Tmedio = tMedio
            };

            report.Add(oneModel);
        }        
        return report;
    }

    private DateTime GetDatetimeFromFilename (string filename)
    {
        int year = Convert.ToInt16(filename.Substring(0,4));
        int month = Convert.ToInt16(filename.Substring(4,2));
        int day = Convert.ToInt16(filename.Substring(6,2));

        return new DateTime(year,month, day);
    }

    private List<string> SCM1FilenameGenerator(string startTime, string endTime)
    {
        //converti start e end in DateTime
        //calcola la distanza in giorni
        //per ogni giorno      
        List<string> fileNames = new List<string>();  
        GeneralPurpose general = new GeneralPurpose();

        DateTime start = general.String2DateConverter(startTime);
        DateTime end = general.String2DateConverter(endTime);

        TimeSpan days = end.Date - start.Date;

        for(int x=0; x<days.Days; x++)
        {
            string oneDate = $"{start.AddDays(x).ToString("yyyyMMdd")}.pro";
            fileNames.Add(oneDate);
        }

        return fileNames;
    }

    private List<DayStatistic> SCM1Mapper (List<SCM1ReportModel> inputList)
    {
        List<DayStatistic> result = new List<DayStatistic>();

        //da inputlist devo tirare fuori tutti i giorni che contiene
        List<DateTime> dates = inputList.Select(d => d.ReferenceDay).Distinct().ToList();
        //per ogni giorno devo fare la somma dei dati
        foreach(DateTime oneDate in dates)
        {
            TimeSpan startTime = inputList.Where(r => r.ReferenceDay == oneDate).Select(s => s.Start).Min();
            TimeSpan endTime = inputList.Where(r => r.ReferenceDay == oneDate).Select(s => s.Stop).Max();
            double progsToday = inputList.Where(r => r.ReferenceDay == oneDate).Select(q => q.Quantita).Sum();
            
            TimeSpan timeOn = new TimeSpan(inputList.Where(r => r.ReferenceDay == oneDate).Sum(t => t.Ttotale.Ticks));
            TimeSpan timeWorking = new TimeSpan(inputList.Where(r => r.ReferenceDay == oneDate).Sum(t => t.Teffettivo.Ticks));

            double progByHour = (progsToday / (timeOn.TotalMinutes + timeWorking.TotalMinutes)) * 60;

            DateTime timePowerOn = new DateTime(oneDate.Year, oneDate.Month, oneDate.Day, startTime.Hours, startTime.Minutes, startTime.Seconds);
            DateTime timePowerOff = new DateTime(oneDate.Year, oneDate.Month, oneDate.Day, endTime.Hours, endTime.Minutes, endTime.Seconds);

            DayStatistic oneDay = new DayStatistic()
            {
                StartTime = timePowerOn,
                EndTime = timePowerOff,
                ProgramsToday = Convert.ToInt16(progsToday),
                TimeOn = timeOn,
                TimeWorking = timeWorking,
                ProgramsPerHour = progByHour,
                TimePowerOn = timePowerOn,
                TimePowerOff = timePowerOff,
                ThicknessPieces = new List<KeyValuePair<int, double>>(){new KeyValuePair<int, double>(1,1)},
                TotalMeters = 1,
                TotalMetersConsumed = 1,
                IsAlive = true                  
            };
            result.Add(oneDay);
        }

        return result;
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
                switch (machineType)
                {
                    case "SCM2":
                        onTime.Add(0);
                        break;
                    
                    case "BIESSE2":
                        onTime.Add(0);
                        break;

                    case "SCM1":
                        onTime.Add(Convert.ToInt32(oneStat.TimeOn.TotalMinutes));
                        break;
                        
                    default:
                        onTime.Add(Convert.ToInt32(oneStat.TimeOn.TotalMinutes));
                        break;
                }
                
                workingTime.Add(Convert.ToInt32(oneStat.TimeWorking.TotalMinutes));
                daysNames.Add(oneStat.StartTime.ToString("dd MMM"));
                progsPerDay.Add(oneStat.ProgramsToday);
                progsPerHour.Add(Math.Round(oneStat.ProgramsPerHour,1));
                totalMetersConsumed.Add(Math.Round(oneStat.TotalMetersConsumed,2));
                totalMeters.Add(Math.Round(oneStat.TotalMeters,2));
            }
        }

        public List<HourStatistics> FormatBIESSE2MachineDailyData(MachineDetails oneMachine, string startTime, string endTime, out List<string> xLabels, out string title)
        {
            List<HourStatistics> result = new List<HourStatistics>();
            xLabels = new List<string>();
            title ="";
            DateTime today = Convert.ToDateTime(startTime);

            //attenzione: per avere i dettagli della giornata occorre riestrarre i dati grezzi
            DatabaseAccessor db = new DatabaseAccessor();
            List<AkronDataRaw> rawData = db.Queryer<AkronDataRaw>(oneMachine.DbDataSource, oneMachine.DbTable);
            List<AkronData> dayData = AkronDataRaw2AkronDataMapper(rawData, startTime, endTime); 

            List<int> hoursInInterval = dayData.Select(h => h.Date.Hour).Distinct().ToList();
            //____ composizione titolo pagina --------------------
            string oraInizio = dayData.Min(d => d.Date).ToString("HH:mm:ss");
            string oraFine = dayData.Max(d=> d.Date).ToString("HH:mm:ss");
            int latiTotali = dayData.Count();
            
            double metriInizioGiornata = dayData[0].TotalMeters;
            double metriFineGiornata = dayData[dayData.Count-1].TotalMeters;
            
            double metriTotali = Math.Round(metriFineGiornata - metriInizioGiornata,2);

            //calcola gli spessori bordati oggi
            List<string> spessori = dayData.Where(e => e.Descr == "PANEL ENTERED")
                                            .Select(s => s.E)
                                            .Distinct().ToList();
            
            string spessoriOggi = String.Join(", ", spessori);

            title =$"orario di lavoro: {oraInizio}-{oraFine}, {latiTotali} lati, {metriTotali.ToString().Replace(',','.')} metri bordati, spessori: {spessoriOggi}";

            //===============================
            //qui possiamo conteggiare i pannelli bordati per ogni ora
            
            foreach(int oneHour in hoursInInterval)
            {
                DateTime thisHourStart = new DateTime(today.Year, today.Month, today.Day, oneHour, 0, 0);
                DateTime thisHourEnd = new DateTime(today.Year, today.Month, today.Day, oneHour, 59, 59);

                List<AkronData> oneHourData = dayData.Where(d => d.Date >= thisHourStart && d.Date <= thisHourEnd).ToList();  

                TimeSpan debugStart = new TimeSpan(oneHour, 0, 0);
                TimeSpan debugEnd = new TimeSpan(oneHour, 59, 59);

                List<AkronData> debug2 = dayData.Where(d => d.Date.TimeOfDay >= debugStart && d.Date.TimeOfDay <= debugEnd).ToList();               
                
                string intervalMax = oneHourData.Max(d => d.Date.ToString("HH:mm"));
                string intervalMin = oneHourData.Min(d => d.Date.ToString("HH:mm"));

                string label2add = $"{intervalMin}-{intervalMax}";
                if(!xLabels.Contains(label2add)) xLabels.Add(label2add);

                //metri bordati in quell'ora                
                double metriInizioOra = oneHourData[0].TotalMeters;
                double metriFineOra = oneHourData[oneHourData.Count-1].TotalMeters;
                
                double metersThisHour = Math.Round(metriFineOra - metriInizioOra, 2);

                //lati bordati in quell'ora
                int latiInizioOra = oneHourData[0].TotalPanels;
                int latiFineOra = oneHourData[oneHourData.Count-1].TotalPanels;

                int sidesThisHour = latiFineOra - latiInizioOra;                

                HourStatistics oneHourStat = new HourStatistics() 
                {
                    Hour = oneHour,
                    TotalMeters = metersThisHour,
                    TotalSides = sidesThisHour,
                    ConsumedMeters = 0,
                    Thickness = 0                   
                };

                result.Add(oneHourStat);
            }

            return result;
        }

        public List<HourStatistics> FormatSCM2MachineDailyData(MachineDetails oneMachine, string startTime, string endTime, out List<string> xLabels, out string title)
        {
            List<HourStatistics> result = new List<HourStatistics>();
            xLabels = new List<string>();
            //attenzione: per avere i dettagli della giornata occorre riestrarre i dati grezzi
            List<SCM2ReportBody> dayData = GetSCM2WebRawData(oneMachine, startTime, endTime);
            

            List<int> hoursInInterval = dayData.Select(h => h.DateTime.Hour).Distinct().ToList();
            //____ composizione titolo pagina --------------------
            string oraInizio = dayData.Min(d => d.DateTime).ToString("HH:mm");
            string oraFine = dayData.Max(d=> d.DateTime).ToString("HH:mm");
            int latiTotali = dayData.Count();
            double metriTotali = Math.Round(dayData.Sum(m => m.Length)/1000,1);
            double metriConsumati = Math.Round(dayData.Sum(m=> m.EdgeConsumptionLH)/1000,1);

            title =$"orario di lavoro: {oraInizio}-{oraFine}, {latiTotali} lati, {metriTotali.ToString().Replace(',','.')} metri bordati, {metriConsumati.ToString().Replace(',','.')} bordo consumato";

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

                    double totalMeters = Math.Round(oneHourData.Sum(t => t.Length)/1000,1);
                    int totalSides = oneHourData.Count();
                    double consumedMeters = Math.Round(oneHourData.Sum(c => c.EdgeConsumptionLH)/1000,1);

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

        public List<HourStatistics> FormatBIESSE1MachineDailyData(MachineDetails oneMachine, string startTime, string endTime, out List<string> biesseXLabels, out string biessetitle)
        {
            GeneralPurpose genP = new GeneralPurpose();
            FtpService ftp = new FtpService(oneMachine.ServerAddress, oneMachine.UserName, genP.ImplicitPwd(oneMachine.UserName));
            List<HourStatistics> result = new List<HourStatistics>();
            
            biesseXLabels = new List<string>();
            biessetitle ="";

            string oneFilename = $"P_{startTime.Replace('-','_')}.xls";
            string localFile = Path.Combine(oneMachine.FtpTempFolder, oneFilename);
            ftp.FtpDownloadFile($"/{oneMachine.MachineName}/{oneFilename}/", localFile);
            List<BiesseReportModel> reportList = GetReportContent(localFile);            

            //-----analisi dati e creazione --------
            DateTime dayStart = reportList.Min(d => d.StartDate);
            DateTime dayEnd = reportList.Max(d => d.EndDate);
            
            //modificare per conteggio nesting
            //int pezziTotali = reportList.Count();
            int pezziTotali = GetBIESSE1ReportListTotalPieces(reportList);
            
            TimeSpan totalWorktime = dayEnd-dayStart;
            double minuti = totalWorktime.TotalMinutes;
            biessetitle = $"orario di lavoro: {dayStart.ToString("HH:mm")}-{dayEnd.ToString("HH:mm")}, {pezziTotali} pezzi, {Math.Round(pezziTotali/minuti*60, 2)} pezzi/ora";

            List<int> hoursInInterval = reportList.Select(h => h.StartDate.Hour).Distinct().ToList();
            DateTime today = Convert.ToDateTime(startTime);

            foreach(int oneHour in hoursInInterval)
            {
                DateTime thisHourStart = new DateTime(today.Year, today.Month, today.Day, oneHour, 0, 0);
                DateTime thisHourEnd = new DateTime(today.Year, today.Month, today.Day, oneHour, 59, 59);
                
                List<BiesseReportModel> thisHourData = reportList.Where(d => d.StartDate.Hour == oneHour).ToList();

                var thisHourLastEnd = thisHourData.Max(d=> d.StartDate.Add(d.Time));

                //fare la lista di tutti i programmi dove l'ora (solo l'ora è uguale all'ora di inizio)
                //fare il conteggio di quanto si spinge in là 

                List<BiesseReportModel> oneHourData = reportList.Where(d => d.StartDate >= thisHourStart)
                                                                .Where(e => e.EndDate <= thisHourLastEnd).ToList();                
                                                              

                string inizioOra = oneHourData.Min(s => s.StartDate).ToString("HH:mm");
                string fineOra = oneHourData.Max(e => e.EndDate).ToString("HH:mm");

                string label2add = $"{inizioOra}-{fineOra}";

                if(!biesseXLabels.Contains(label2add)) biesseXLabels.Add(label2add);

                var debug = GetBIESSE1ReportListTotalPieces(oneHourData);
                var debug1 = oneHourData.Count();

                HourStatistics oneHourStat = new HourStatistics()
                {
                    Hour = oneHour,
                    TotalMeters = 1,
                    TotalSides = GetBIESSE1ReportListTotalPieces(oneHourData),
                    ConsumedMeters = 1,
                    Thickness = 1
                };
                result.Add(oneHourStat);
            }

            return result;
        }

        public int GetBIESSE1ReportListTotalPieces(List<BiesseReportModel> inputList)
        {
            int result =0;
            foreach(BiesseReportModel oneLine in inputList)
            {
                if(oneLine.HasNesting)
                {
                    result += oneLine.TotalPieces;
                }
                else
                {
                    result++;
                }
            }
            return result;
        }

        public List<BIESSE1ReportBody> BIESSE1ReportMapper(List<DayStatistic> inputList)
        {
            List<BIESSE1ReportBody> report= new List<BIESSE1ReportBody>();

            foreach(DayStatistic oneStat in inputList)
            {
                TimeSpan tempoTotale = Convert.ToDateTime(oneStat.EndTime) - Convert.ToDateTime(oneStat.StartTime);
                double minutiLavoro = oneStat.TimeWorking.TotalMinutes;
                double minutiAccensione = oneStat.TimeOn.TotalMinutes;
                double minutiTotali = tempoTotale.TotalMinutes;
                
                BIESSE1ReportBody oneReport = new BIESSE1ReportBody()
                {
                    Giorno = oneStat.StartTime.ToString("dd/MM/yyyy"),
                    OraInizio = oneStat.StartTime.ToString("HH:mm:ss"),
                    OraFine = oneStat.EndTime.ToString("HH:mm:ss"),
                    TempoAccensione = oneStat.TimeOn.ToString(@"hh\:mm\:ss"),
                    TempoLavoro = oneStat.TimeWorking.ToString(@"hh\:mm\:ss"),
                    TempoTotale = tempoTotale.ToString(@"hh\:mm\:ss"),
                    MinutiAccensione = Math.Round(minutiAccensione,1).ToString(),
                    MinutiLavoro = Math.Round(minutiLavoro, 1).ToString(),
                    MinutiTotali = Math.Round(minutiTotali,1).ToString(),
                    ProgrammiEseguiti = oneStat.ProgramsToday,
                    ProgrammiOra = oneStat.ProgramsPerHour
                };
                report.Add(oneReport);
            }
            return report;
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