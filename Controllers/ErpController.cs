using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using mes.Models.Services.Infrastructures;
using mes.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mes.Models.InfrastructureModels;
using System.IO;
using Newtonsoft.Json;

namespace mes.Controllers
{
    public class ErpController : Controller
    {
        private readonly ILogger<ErpController> _logger;
        private readonly string connectionString ="Data Source=../mesData/erpdata.db";

        private readonly string usersDbConnString ="Data Source=../data/app.db";
        const string intranetLog=@"c:\temp\intranet.log";

        bool aggiornaDipendenti = false;
        bool aggiornaPermessi = false;

        public ErpController(ILogger<ErpController> logger)
        {
            _logger = logger;         
        }

        public IActionResult Main()
        {
            UserData userData = GetUserData();            
            Log2File(JsonConvert.SerializeObject(userData));
            Log2File($"-----------------ErpController");

            return View();
        }

        #region Dipendenti

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheLeggi, AnagraficheScrivi")]
        public IActionResult MainDipendenti()
        {
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti")
                                            .Where(x => x.Enabled =="1")
                                            .OrderBy(y => y.Cognome).ToList();

            return View(dipendenti);

        }

        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult AggiornaDipendenti(List<DipendenteViewModel> Dipendenti)
        {
            if(aggiornaDipendenti)
            {                
                UserData userData = GetUserData();
                DatabaseAccessor dbAccessor = new DatabaseAccessor();
                foreach(DipendenteViewModel oneModel in Dipendenti)
                {
                    oneModel.CreatedBy = userData.UserName;
                    oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                    int result = dbAccessor.Updater<DipendenteViewModel>(connectionString, "Dipendenti", oneModel, oneModel.id);
                }
            }       
            return RedirectToAction("MainDipendenti");
        }

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult InsertDipendente(string errorMessage)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti")
                                        .Where(x => x.Enabled=="1")
                                        .OrderBy(y =>y.Cognome).ToList();            
            
            ViewBag.DipendentiList = dipendenti;

            List<string> usernames = dipendenti.Select(x => x.Username).ToList();
            List<string> matricole = dipendenti.Select(z => z.Matricola).ToList();

            ViewBag.usernames = usernames;
            ViewBag.matricole = matricole;
            ViewBag.errorMessage = errorMessage;

            // passare la viewbag degli username e dei notify address presa da app.db/aspnetusers
            // fare la differenza con quelli già presi in erpdata/dipendenti

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult InsertDipendente(DipendenteViewModel newDipendente)
        {
            UserData userData = GetUserData();

            newDipendente.CreatedBy = userData.UserName;
            newDipendente.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            newDipendente.Enabled = "1";            

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");

            long max = (from l in dipendenti select l.id).Max();

            newDipendente.id = max + 1;

            int result = dbAccessor.Insertor<DipendenteViewModel>(connectionString, "Dipendenti", newDipendente);

            return RedirectToAction("MainDipendenti");
        }

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult ModDipendente(long id)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> Dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti")
                                        .Where(x => x.Enabled=="1").ToList(); 
            ViewBag.DipendentiList = Dipendenti;
            DipendenteViewModel oneModel = Dipendenti.Where(x => x.id == id).FirstOrDefault();

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult ModDipendente(DipendenteViewModel oneModel)
        {
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Enabled = "1";

            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            int result = dbAccessor.Updater<DipendenteViewModel>(connectionString,"Dipendenti", oneModel, oneModel.id);

            return RedirectToAction("MainDipendenti");
        }        

        [HttpGet]
        [Authorize(Roles = "root, AnagraficheScrivi")]
        public IActionResult CancDipendente(long id)
        {
            aggiornaDipendenti = false;
            DatabaseAccessor dbAccessor = new DatabaseAccessor();            
            //int result = dbAccessor.Delete(connectionString, "Dipendenti", id);

            DipendenteViewModel Dipendente2disable = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti").Where(x => x.id == id).FirstOrDefault();
            Dipendente2disable.Enabled = "0";

            int result = dbAccessor.Updater<DipendenteViewModel>(connectionString,"Dipendenti", Dipendente2disable, id);
            
            Thread.Sleep(1000);
            return RedirectToAction("MainDipendenti");
        }

        #endregion

        #region Permessi

        [HttpGet]
        [Authorize(Roles = "User, root, PermessiInsertor, PermessiSupervisor, PermessiMaster")]
        public IActionResult MainPermessi()
        {
            int actualMonth = DateTime.Today.Month;
            int actualYear = DateTime.Today.Year;
            int lastDay = DateTime.DaysInMonth(actualYear,actualMonth);

            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;
            List<PermessoViewModel> permessi = new List<PermessoViewModel>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            bool level1 = false;
            if(userData.UserRoles.Contains("root")||
                userData.UserRoles.Contains("PermessiSupervisor")||
                userData.UserRoles.Contains("PermessiMaster")) level1=true;

            if(level1)
            {
                permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                    .Where(x => x.Enabled =="1")
                                    .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime($"{actualYear}-{actualMonth}-01").Date)
                                    .Where(w => Convert.ToDateTime(w.DataFine).Date <= Convert.ToDateTime($"{actualYear}-{actualMonth}-{lastDay}").Date)                                            
                                    .OrderBy(y => y.DataInizio).ToList();                                    
            }
            else if(userData.UserRoles.Contains("User"))
            {
                permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                    .Where(x => x.Enabled =="1")
                                    .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime($"{actualYear}-{actualMonth}-01").Date)
                                    .Where(w => Convert.ToDateTime(w.DataFine).Date <= Convert.ToDateTime($"{actualYear}-{actualMonth}-{lastDay}").Date)                                               
                                    .Where(y => y.Username == userData.UserName)
                                    .OrderBy(y => y.DataInizio).ToList();
            }

            List<DipendenteViewModel> dipendenti = (List<DipendenteViewModel>)dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");
            List<DipendenteViewModel>dipendentiOrdered =  dipendenti.Where(e => e.Enabled == "1").OrderBy(z => z.Cognome).ToList();
            DipendenteViewModel dipendente = dipendenti.Where(x => x.Username == userData.UserName).FirstOrDefault();
            ViewBag.dipendente = dipendente;
            ViewBag.dipendenti = dipendentiOrdered;

            ViewBag.userName = userData.UserName;
            //aggiornaPermessi = true;
            
            //richiesta di vedere sul calendario sempre tutti i permessi
            List<PermessoViewModel> allPermissions = new List<PermessoViewModel>();
            if(level1)
            {
                allPermissions = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                                                    .Where(p => p.Enabled == "1").ToList();
            }
            else
            {
            allPermissions = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                                                .Where(u => u.Username == userData.UserName)
                                                                .Where(p => p.Enabled == "1").ToList();                
            }


            List<CalendarEvent> events = PermessiExtractor(allPermissions);
            //List<CalendarEvent> events = PermessiExtractor(permessi);
            ViewBag.calendarEvents = events; 
            ViewBag.permessi = permessi;
            List<string> tipologie = new List<string>();
            tipologie.Add("permesso");
            tipologie.Add("ferie");
            tipologie.Add("malattia");

            ViewBag.tipologie = tipologie;                            

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User, root, PermessiInsertor, PermessiSupervisor, PermessiMaster")]
        public IActionResult MainPermessi(PermessoFilter filter)
        {
            GeneralPurpose genP = new GeneralPurpose();
            UserData userData = GetUserData();
            ViewBag.userRoles = userData.UserRoles;

            List<PermessoViewModel> permessi = new List<PermessoViewModel>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            bool level1 = false;
            if(userData.UserRoles.Contains("root")||
                userData.UserRoles.Contains("PermessiMaster")||
                userData.UserRoles.Contains("PermessiSupervisor")) level1=true;

            if(level1)
            {
                if(filter.Tipologia == null && filter.Username !=null) //chiedo un dipendente ma non la tipologia(permesso, ferie, etc)
                {
                    permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                        .Where(x => x.Enabled =="1")
                                        .Where(y => y.Username == filter.Username)
                                        .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime(filter.DataInizio).Date)
                                        .Where(w => Convert.ToDateTime(w.DataFine).Date <= Convert.ToDateTime(filter.DataFine).Date)
                                        .ToList();
                }
                else if(filter.Username != null && filter.Tipologia != null) // chiedo un dipendente e anche la tipologia di permesso/ferie
                {                     
                    permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                        .Where(x => x.Enabled =="1")
                                        .Where(y => y.Username == filter.Username)
                                        .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime(filter.DataInizio).Date)
                                        .Where(j => Convert.ToDateTime(j.DataFine).Date <= Convert.ToDateTime(filter.DataFine).Date)
                                        .Where(k => k.Tipologia == filter.Tipologia)
                                        .ToList();
                }
                else if(filter.Username == null && filter.Tipologia == null) //non chiedo nulla, restituisce tutta la lista di quel periodo
                {                   
                    permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                        .Where(x => x.Enabled =="1")
                                        .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime(filter.DataInizio).Date)
                                        .Where(y => Convert.ToDateTime(y.DataFine).Date <= Convert.ToDateTime(filter.DataFine).Date)                                                
                                        .ToList();
                }
                else if(filter.Username == null && filter.Tipologia != null) // tutta la lista di quella tipologia per tutti
                {
                    permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                        .Where(x => x.Enabled =="1")
                                        .Where(z => Convert.ToDateTime(z.DataInizio).Date >= Convert.ToDateTime(filter.DataInizio).Date)
                                        .Where(y => Convert.ToDateTime(y.DataFine).Date <= Convert.ToDateTime(filter.DataFine).Date)
                                        .Where(w => w.Tipologia == filter.Tipologia)
                                        .ToList(); 
                }
            }
            else if(userData.UserRoles.Contains("User"))
            {
                permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                            .Where(x => x.Enabled =="1")
                                            .Where(y => y.Username == userData.UserName).ToList();
            }

            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");
            DipendenteViewModel dipendente = dipendenti.Where(x => x.Username == userData.UserName).FirstOrDefault();
            ViewBag.dipendente = dipendente;
            ViewBag.dipendenti = dipendenti;

            ViewBag.userName = userData.UserName;
            //aggiornaPermessi = true;
            
            List<CalendarEvent> events = PermessiExtractor(permessi);
            ViewBag.calendarEvents = events; 
            ViewBag.permessi = permessi;
            List<string> tipologie = new List<string>();
            tipologie.Add("permesso");
            tipologie.Add("ferie");
            tipologie.Add("malattia");

            ViewBag.tipologie = tipologie;   

            return View();
        }
        private List<CalendarEvent> PermessiExtractor(List<PermessoViewModel> permessi)
        {
            GeneralPurpose genP = new GeneralPurpose();
            List<CalendarEvent> permessiOnCalendar = new List<CalendarEvent>();
            foreach(var permesso in permessi)
            {
                CalendarEvent oneEvent = new CalendarEvent(){
                    Title = $"{permesso.Nome} {permesso.Cognome}",
                    //StartDate = genP.PermessiDate2Calendar(permesso.DataInizio),
                    StartDate = Convert.ToDateTime(permesso.DataInizio),
                    //EndDate = genP.PermessiDate2Calendar(permesso.DataFine),
                    EndDate = Convert.ToDateTime(permesso.DataFine),
                    Color = GetPermessoColor(permesso.Stato)
                };
                permessiOnCalendar.Add(oneEvent);
            }

            return permessiOnCalendar;
        }

        private string GetPermessoColor(string statoPerm)
        {
            string color ="";
            switch (statoPerm)
            {
                case "in attesa":
                color ="orange" ;
                break;

                case "approvato":
                color ="green" ;
                break;

                case "respinto":
                color ="red" ;
                break;                                     
            }
            return color;
        }

        [Authorize(Roles = "root, PermessiMaster")]
        public IActionResult AggiornaPermessi(List<PermessoViewModel> Permessi)
        {          
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            foreach(PermessoViewModel oneModel in Permessi)
            {
                oneModel.CreatedBy = userData.UserName;
                oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
                int result = dbAccessor.Updater<PermessoViewModel>(connectionString, "Permessi", oneModel, oneModel.id);
            }
  
            return RedirectToAction("MainPermessi");
        }

        [HttpGet]
        [Authorize(Roles = "root, User, PermessiInsertor, PermessiSupervisor, PermessiMaster")]
        public IActionResult InsertPermesso()
        {
            UserData userData = GetUserData();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PermessoViewModel> Permessi = new List<PermessoViewModel>();
            
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");
            DipendenteViewModel dipendente = dipendenti.Where(x => x.Username == userData.UserName).FirstOrDefault();

            // se utente, vede solo la lista dei suoi
            if(userData.UserRoles.Contains("root")||userData.UserRoles.Contains("PermessiMaster")||userData.UserRoles.Contains("PermessiSupervisor"))
            {
                Permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                            .Where(x => x.Enabled=="1")
                                            .OrderBy(y => y.DataInizio).ToList(); 
            } else if (userData.UserRoles.Contains("User"))
            {
                Permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                            .Where(x => x.Enabled=="1")
                                            .Where(y => y.Username == userData.UserName)
                                            .OrderBy(z => z.DataInizio).ToList(); 
            } else if(userData.UserRoles.Contains("PermessiInsert"))
            {
                List<DipendenteViewModel> allDipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");
                ViewBag.allDipendenti = allDipendenti.OrderBy(x => x.Cognome);
            }
            
            ViewBag.Dipendente = dipendente;
            ViewBag.PermessiList = Permessi;
            ViewBag.UserRoles = userData.UserRoles;
            ViewBag.allDipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");
            ViewBag.userName = userData.UserName;

            List<string> tipologie = new List<string>();
            tipologie.Add("permesso");
            tipologie.Add("ferie");
            tipologie.Add("malattia");

            ViewBag.tipologie = tipologie;             

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "root, User, PermessiInsertor, PermessiSupervisor, PermessiMaster")]
        public IActionResult InsertPermesso(PermessoViewModel newPermesso)
        {
            UserData userData = GetUserData();

            newPermesso.CreatedBy = userData.UserName;
            newPermesso.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            newPermesso.DataDiRichiesta = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            GeneralPurpose genPurpose = new GeneralPurpose();
            newPermesso.IntervalloTempo = genPurpose.PermissionTimeSpan(newPermesso.DataInizio, newPermesso.DataFine, newPermesso.Tipologia);
            newPermesso.Stato = "in attesa";
            if(newPermesso.Note == null) newPermesso.Note ="nessuna";

            newPermesso.DataInizio = Convert.ToDateTime(newPermesso.DataInizio).ToString("dd/MM/yyyy HH:mm");
            newPermesso.DataFine = Convert.ToDateTime(newPermesso.DataFine).ToString("dd/MM/yyyy HH:mm");

            //ottieni il nome utente per lo user che richiede l'inserimento            
            //se il nome nel model != nome dello user => ricerca lo username del dipendente per il quale viene chiesto il permesso
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            DipendenteViewModel intermediaryUser = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti")
                                                                    .Where(x => x.Username == userData.UserName).FirstOrDefault();

            if(intermediaryUser.Nome != newPermesso.Nome) 
            {
                DipendenteViewModel applicantUser = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti")
                                                                        .Where(x => x.Nome == newPermesso.Nome)
                                                                        .Where(y => y.Cognome == newPermesso.Cognome).FirstOrDefault();
                newPermesso.Username = applicantUser.Username;
            }
            else newPermesso.Username = userData.UserName;
            
            newPermesso.Enabled = "1";            
            List<PermessoViewModel> Permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi");

            long max = (from l in Permessi select l.id).Max();
            newPermesso.id = max + 1;
            int result = dbAccessor.Insertor<PermessoViewModel>(connectionString, "Permessi", newPermesso);

            return RedirectToAction("MainPermessi");
        }

        [HttpGet]
        [Authorize(Roles = "root, PermessiMaster, User")]
        public IActionResult ModPermesso(long id)
        {
            UserData userData = GetUserData();
            bool authorize =    (userData.UserRoles.Contains("root")|
                                userData.UserRoles.Contains("PermessiMaster")|
                                userData.UserRoles.Contains("root")) ? true : false;

            List<string> statiPermessi = new List<string>();
            statiPermessi.Add("in attesa");
            statiPermessi.Add("approvato");
            statiPermessi.Add("respinto");

            ViewBag.statiPermessi = statiPermessi;

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PermessoViewModel> Permessi = new List<PermessoViewModel>();
            if(authorize)
            {
                Permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                            .Where(x => x.Enabled=="1").ToList(); 
            } else {
                Permessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                            .Where(u => u.Username == userData.UserName)
                                            .Where(x => x.Enabled=="1").ToList();                 
            }
            ViewBag.PermessiList = Permessi;
            PermessoViewModel oneModel = Permessi.Where(x => x.id == id).FirstOrDefault();

            if(oneModel.Stato == "in attesa") ViewBag.selectedStatus = 1;
            if(oneModel.Stato == "approvato") ViewBag.selectedStatus = 2;
            if(oneModel.Stato == "respinto") ViewBag.selectedStatus = 3;            

            ViewBag.startDatePermesso = DateToInputDateFormat(oneModel.DataInizio);
            ViewBag.endDtaePermesso = DateToInputDateFormat(oneModel.DataFine);
            ViewBag.userRoles = userData.UserRoles;

            return View(oneModel);
        }

        [HttpPost]
        [Authorize(Roles = "root, PermessiMaster, User")]
        public IActionResult ModPermesso(PermessoViewModel oneModel)
        {                        
            UserData userData = GetUserData();

            oneModel.CreatedBy = userData.UserName;
            oneModel.CreatedOn = DateTime.Now.ToString("dd/MM/yyyy-HH:mm");
            //oneModel.Stato = "in attesa";

            string actualDataInizio = oneModel.DataInizio;
            oneModel.DataInizio = DateToDbDateFormat(actualDataInizio);
            string actualDataFine = oneModel.DataFine;
            oneModel.DataFine = DateToDbDateFormat(actualDataFine);
            
            //ricalcolo durata
            GeneralPurpose genPurpose = new GeneralPurpose();
            oneModel.IntervalloTempo = genPurpose.PermissionTimeSpan(oneModel.DataInizio, oneModel.DataFine, oneModel.Tipologia);

            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<PermessoViewModel> actualPermessi = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi")
                                        .Where(x => x.Enabled =="1").ToList();           

            int result = dbAccessor.Updater<PermessoViewModel>(connectionString,"Permessi", oneModel, oneModel.id);

            //notifica ai PermMaster se un permesso ha cambiato stato
            string previousState = actualPermessi.Where(x => x.id == oneModel.id).Select(y => y.Stato).FirstOrDefault();
            string actualState = oneModel.Stato;
            if (previousState!=actualState)
            {
                EmailSender emailSender = new EmailSender();
                //recupera la lista di tutti gli utenti con ruolo PermMaster
                List<UsersRolesNotify> allUsersRolesNotify = GetCompleteUsersList();
                List<string> notifyList = allUsersRolesNotify.Where(x => x.UserRoles.ToString().Contains("PermessiMaster"))
                                                                            .Select( z=> z.UserNotifyAddress).ToList();
                
                string subject = $"cambio stato permesso: {oneModel.Nome} {oneModel.Cognome} -> {oneModel.Stato}";
                string body = $"in data {DateTime.Now} l'utente {userData.UserName} ha cambiato lo stato della richiesta di {oneModel.Tipologia} fatta da:\n\n{oneModel.Nome} {oneModel.Cognome}" +
                $"\nper il periodo\n{oneModel.DataInizio} -> {oneModel.DataFine}\nda:{previousState}\na:{actualState}";

                foreach(string oneRecipient in notifyList)
                {
                   if(IsValidEmail(oneRecipient)) emailSender.SendEmail(subject, body, oneRecipient, "automation@intranet");
                }
            }
            return RedirectToAction("MainPermessi");
        } 


        private string DateToInputDateFormat(string inputString)
        {
            string day = inputString.Substring(0,2);
            string month = inputString.Substring(3,2);
            string year = inputString.Substring(6,4);
            string hour = inputString.Substring(11,2);
            string minutes = inputString.Substring(14,2);

            return $"{year}-{month}-{day}T{hour}:{minutes}";
        }

        private string DateToDbDateFormat(string inputString)
        {
            string year = inputString.Substring(0,4);
            string month = inputString.Substring(5,2);
            string day = inputString.Substring(8,2);
            string hour = inputString.Substring(11,2);
            string minutes = inputString.Substring(14,2);

            return $"{day}/{month}/{year}-{hour}:{minutes}";
        }        
        
        private bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith(".")) {
                return false; 
            }
            try {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch {
                return false;
            }            
        }

        //[HttpGet]
        //[Authorize(Roles = "root, PermessiMaster")]
        //public IActionResult CancPermesso(long id)
        //{
        //    aggiornaPermessi = false;
        //    DatabaseAccessor dbAccessor = new DatabaseAccessor();            
        //    //int result = dbAccessor.Delete(connectionString, "Permessi", id);
//
        //    PermessoViewModel Permesso2disable = dbAccessor.Queryer<PermessoViewModel>(connectionString, "Permessi").Where(x => x.id == id).FirstOrDefault();
        //    Permesso2disable.Enabled = "0";
//
        //    int result = dbAccessor.Updater<PermessoViewModel>(connectionString,"Permessi", Permesso2disable, id);
        //    
        //    Thread.Sleep(1000);
        //    return RedirectToAction("MainPermessi");
        //}

        public async Task<IActionResult> GeneratePdf(List<PermessoViewModel> objectList, string username)
        {
            List<PermessoViewModel> permessi = (List<PermessoViewModel>)TempData["permessi"];
            PdfGenerator pdfGen = new PdfGenerator();
            byte[] document = pdfGen.GeneratePermessiPdf(objectList);

                Response.ContentType = "application/pdf";                
                Response.Headers.Add("Content-Disposition", "attachment;filename=permessi.pdf");

                // Write the PDF to the response stream
                await Response.Body.WriteAsync(document, 0, document.Length);
                await Response.Body.FlushAsync();
                await Response.CompleteAsync();

            return View("MainPermessi");
        }

        private List<DipendenteViewModel> GetPermMasterUsers()
        {
            // prendere la lista di tutti i 
            List<UsersRolesNotify> allUsers = GetCompleteUsersList();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            List<DipendenteViewModel> dipendenti = dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");

            List<DipendenteViewModel> dipPermMaster = dipendenti.Where(x => x.Ruolo.Contains("PermMaster")).ToList();

            return dipPermMaster;
        }

        //private List<string> GetPermMasterNorifyAddress(List<UsersRolesNotify> allUsersRolesNotify)
        //{
        //    List<string> notifyList = new List<string>();
//
        //    //notifyList = allUsersRolesNotify.Where(x => x.UserRoles.ToString().Contains)
//
        //    return notifyList;
        //}

        #endregion

        private UserData GetUserData()
        {
            UserData userData = new UserData();

            string userRoles="";
            ViewBag.userId =  User.FindFirstValue(ClaimTypes.NameIdentifier); // will give the user's userId
            userData.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.userName =  User.FindFirstValue(ClaimTypes.Name); // will give the user's userName
            userData.UserName = User.FindFirstValue(ClaimTypes.Name);

            IEnumerable<Claim> roles = User.FindAll(ClaimTypes.Role);
            foreach(var role in roles)
            {
                userRoles += $"{role.Value}, ";
            }
            userData.UserRoles = userRoles;
            
            ViewBag.userEmail =  User.FindFirstValue(ClaimTypes.Email); // will give the user's Email
            userData.UserEmail = User.FindFirstValue(ClaimTypes.Email);

            ViewBag.userRoles= userRoles;                    

            //ViewBag.address = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString().Substring(7);
            ViewBag.address = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            ViewBag.port = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            //userData.UserIpAddress = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString().Substring(7);
            userData.UserIpAddress = HttpContext.Features.Get<IHttpConnectionFeature>().RemoteIpAddress.ToString();
            userData.UserIpPort = HttpContext.Features.Get<IHttpConnectionFeature>().RemotePort.ToString();

            return userData;
        }

        private List<UsersRolesNotify> GetCompleteUsersList()
        {   
            List<UsersRolesNotify> output = new List<UsersRolesNotify>();
            DatabaseAccessor dbAccessor = new DatabaseAccessor();

            List<UserDataModel> allUsersData = (List<UserDataModel>)dbAccessor.Queryer<UserDataModel>(usersDbConnString, "AspNetUsers");            
            //List<AspNetRoles> allRoles = (List<AspNetRoles>)dbAccessor.Queryer<AspNetRoles>(usersDbConnString,"AspNetRoles");
            //List<AspNetUsersRoles> allUsersRoles = (List<AspNetUsersRoles>)dbAccessor.Queryer<AspNetUsersRoles>(usersDbConnString,"AspNetUserRoles");
            List<DipendenteViewModel> dipendenti = (List<DipendenteViewModel>)dbAccessor.Queryer<DipendenteViewModel>(connectionString, "Dipendenti");

            foreach(UserDataModel oneUserData in allUsersData)
            {
                UsersRolesNotify oneURN = new UsersRolesNotify()
                {
                    Username = oneUserData.UserName,
                    UserRoles = GetUsersRolesString(oneUserData.Id),
                    UserNotifyAddress = dipendenti.Where(z => z.Username == oneUserData.UserName).Select(k => k.NotifyAddress).FirstOrDefault()
                };
                output.Add(oneURN);
            }

            return output;
        }

        private string GetUsersRolesString(string userId)
        {
            DatabaseAccessor dbAccessor = new DatabaseAccessor();
            string output ="";

            //serve lo user Id, da trovare una o più volte dentro la allUsersRoles, per ogni volta si aggiunge il role.name
            //lista di tutte le corrspondenze UserId con RoleId
            List<AspNetUsersRoles> allUsersRoles = (List<AspNetUsersRoles>)dbAccessor.Queryer<AspNetUsersRoles>(usersDbConnString,"AspNetUserRoles");
            
            //lista di tutti i roles.Name e corrispondente roles.Id
            List<AspNetRoles> allRoles = (List<AspNetRoles>)dbAccessor.Queryer<AspNetRoles>(usersDbConnString,"AspNetRoles");

            foreach(AspNetUsersRoles oneRole in allUsersRoles)
            {
                if(oneRole.UserId == userId)
                {
                    List<AspNetRoles> thisUserRoles = allRoles.Where(x => x.Id == oneRole.RoleId).ToList();
                    foreach(AspNetRoles oneUserRole in thisUserRoles)
                    {
                        output +=$"{oneUserRole.Name} ";
                    }
                }
            }

            return output;
        }

        private void Log2File(string line2log)
        {
            using(StreamWriter sw = new StreamWriter(intranetLog, true))
            {
                sw.WriteLine($"{DateTime.Now} -> {line2log}");
            }
        }             
    }
}