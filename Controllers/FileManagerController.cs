// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using mes.Models.ViewModels; 
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace mes.Controllers
{
    public class FileManagerController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FileManagerController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View(new FileUploadViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Upload(FileUploadViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.FileToUpload != null && model.FileToUpload.Length > 0)
                {
                    try
                    {
                        // 1. Construct the file path on the server
                        //string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads"); // "wwwroot/uploads"
                        string uploadsFolder = Path.Combine("c:\\temp", "uploads"); // "wwwroot/uploads"
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.FileToUpload.FileName; // Ensure unique names
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        ViewBag.filename = filePath;
                        // 2. Save the file to the server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.FileToUpload.CopyToAsync(fileStream);
                        }

                        // 3. Optional:  Provide feedback to the user
                        ViewBag.Message = "File caricato con successo in: " + filePath;

                        return View("Index", new FileUploadViewModel());
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Message = "Errore durante il caricamento: " + ex.Message;
                        return View("Index", model);
                    }
                }
                else
                {
                    ViewBag.Message = "Nessun file selezionato o file vuoto.";
                    return View("Index", model);
                }
            }
            else
            {
                // Model state is invalid (e.g., missing required field)
                ViewBag.Message = "Selezionare un file.";
                return View("Index", model);
            }
        }
    }
}