using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using mes.Models.ControllersConfigModels;
using mes.Models.InfrastructureConfigModels;
using mes.Models.StatisticsModels;
using mes.Models.ViewModels;
using Newtonsoft.Json;
using ServiceStack;

namespace mes.Models.Services.Infrastructures
{
    public class StatisticsService
    {
        public List<DayStatistic> GetMachineStats(MachineDetails oneMachine, string startTime, string endTime)
        {
            List<DayStatistic> result = new List<DayStatistic>();
            string machineName = oneMachine.MachineName;
            string retrieveMethod = oneMachine.RetrieveMethod;
            string machineType = oneMachine.MachineType;
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
                    break;

                case "SCM1":
                    break;

                case "ftp":
                    break;
            }

            return result;
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
                    IsAlive = false
                };
                result.Add(oneStat);
                return result;           
            }

            DateTime startT = Convert.ToDateTime(startTime);
            DateTime endT = Convert.ToDateTime(endTime);

            //string startPeriod = $"{startT.Year}-{startT.Month}-{startT.Day}T{startT.Hour}%3A{startT.Minute}%3A{startT.Second}";
            //string endPeriod = $"{endT.Year}-{endT.Month}-{endT.Day}T{endT.Hour}%3A{endT.Minute}%3A{endT.Second}";

            string startPeriod = $"{startT.Year}-{startT.Month.ToString("00")}-{startT.Day.ToString("00")}T04%3A00%3A00";
            string endPeriod = $"{endT.Year}-{endT.Month.ToString("00")}-{endT.Day.ToString("00")}T21%3A00%3A00";

            string requestUrl = $"http://{oneMachine.ServerAddress}:{oneMachine.ServerPort}/api/v1/report/production?from={startPeriod}&to={endPeriod}";

            List<SCM2ResultBody> jsonReply = GetWebResponse(requestUrl);
            // TO DO : gestire errore di connessione o di risposta
            //----------------------------------------------
            //trasformazione dati
            
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

            //27/03/2024
            //qui calcola la somma dei dati per ogni giorno
                TimeSpan timeWorking = new TimeSpan(0);

                if(oneMachine.MachineType == "SCM2") 
                {
                    timeWorking = dayEnd - dayStart;
                }

                double prgPerHour = (progs / timeWorking.TotalMinutes)*60;

                result.Add(new DayStatistic(){
                    StartTime = dayStart,
                    EndTime = dayEnd,
                    ProgramsToday = progs,
                    TimeOn = dayEnd - dayStart,
                    TimeWorking = timeWorking,
                    ProgramsPerHour = Math.Round(prgPerHour,1),
                    IsAlive = true
                });                
            }

            return result;
        }

        public List<SCM2ResultBody> GetWebResponse(string requestUrl)
        {
            List<SCM2ResultBody> results = new List<SCM2ResultBody>();
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

            }

            results = JsonConvert.DeserializeObject<List<SCM2ResultBody>>(StatusCutter(text.Replace("'","-")));

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

        public void FormatMachineData(List<DayStatistic> inputStats,
                                        string machineType,
                                        out List<int>onTime,
                                        out List<int> workingTime,
                                        out List<string> daysNames,
                                        out List<int> progsPerDay,
                                        out List<double> progsPerHour)
        {
            onTime = new List<int>();
            workingTime = new List<int>();
            daysNames = new List<string>();
            progsPerDay = new List<int>();
            progsPerHour = new List<double>();
            
            foreach(DayStatistic oneStat in inputStats)
            {
                if(machineType =="SCM2")
                { onTime.Add(0);}
                else{ onTime.Add(Convert.ToInt32(oneStat.TimeOn.TotalMinutes));}
                
                workingTime.Add(Convert.ToInt32(oneStat.TimeWorking.TotalMinutes));
                daysNames.Add(oneStat.StartTime.ToString("dd MMM"));
                progsPerDay.Add(oneStat.ProgramsToday);
                progsPerHour.Add(oneStat.ProgramsPerHour);
            }
        }
    }
}