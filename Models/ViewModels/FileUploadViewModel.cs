
using Microsoft.AspNetCore.Http; 
using System.ComponentModel.DataAnnotations;

namespace mes.Models.ViewModels
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Per favore seleziona un file")]
        [Display(Name = "Scegli un file:")]
        public IFormFile FileToUpload { get; set; } 
    }
}