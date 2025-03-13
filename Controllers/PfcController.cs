using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace mes.Controllers
{
    //[Route("[controller]")]
    public class PfcController : Controller
    {
        private readonly ILogger<PfcController> _logger;

        public PfcController(ILogger<PfcController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InsertPfc()
        {
            //Customers
            //datasource.db/Clienti
            
            //Operators
            //erpdata.db/Dipendenti
            //refactoring
            //andiamo su datasource.db/Operatori
            
            //Works
            //datasource.db/Lavorazioni

            //test data

            List<string>customers = new List<string>();
            customers.Add("Lago");
            customers.Add("Mp3");
            customers.Add("Brofer");
            ViewBag.Customers = customers;


            List<string> works = new List<string>();
            works.Add("taglio cnc");
            works.Add("bordatura");
            ViewBag.WorkPhases = works;

            List<string> operators = new List<string>();
            operators.Add("Alessandro Comper");
            operators.Add("Paolo Zotti");
            operators.Add("Mirko Fabbris");
            ViewBag.Operators = operators;

            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}