using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace mes.Models.Services
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Validates and uploads a file to the configured directory
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>Result containing success status and path or error message</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file);

        /// <summary>
        /// Validates if a file meets the required criteria (not null, size, extension)
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <returns>Validation result with success status and error message if applicable</returns>
        ValidationResult ValidateFile(IFormFile file);
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }

        public static FileUploadResult CreateSuccess(string filePath)
        {
            return new FileUploadResult
            {
                Success = true,
                FilePath = filePath,
                ErrorMessage = string.Empty
            };
        }

        public static FileUploadResult CreateError(string errorMessage)
        {
            return new FileUploadResult
            {
                Success = false,
                FilePath = string.Empty,
                ErrorMessage = errorMessage
            };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Valid()
        {
            return new ValidationResult { IsValid = true, ErrorMessage = string.Empty };
        }

        public static ValidationResult Invalid(string errorMessage)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
        }
    }
}

