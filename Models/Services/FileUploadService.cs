using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly string _uploadDirectory;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedExtensions;

        public FileUploadService(IConfiguration configuration)
        {
            // Get configuration values with defaults if not specified
            _uploadDirectory = configuration.GetValue<string>("FileUpload:UploadDirectory", @"c:\temp\uploads");
            _maxFileSizeBytes = configuration.GetValue<int>("FileUpload:MaxFileSizeInMB", 10) * 1024 * 1024; // Convert MB to bytes
            _allowedExtensions = configuration.GetSection("FileUpload:AllowedExtensions")
                                .Get<string[]>() ?? new string[] { ".csv" };
        }

        public ValidationResult ValidateFile(IFormFile file)
        {
            // Check if file is null
            if (file == null || file.Length == 0)
            {
                return ValidationResult.Invalid("No file selected or file is empty.");
            }

            // Check file size
            if (file.Length > _maxFileSizeBytes)
            {
                return ValidationResult.Invalid($"File size exceeds the maximum allowed size ({_maxFileSizeBytes / (1024 * 1024)} MB).");
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                return ValidationResult.Invalid($"File extension '{fileExtension}' is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
            }

            return ValidationResult.Valid();
        }

        public async Task<FileUploadResult> UploadFileAsync(IFormFile file)
        {
            // Validate the file
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
            {
                return FileUploadResult.CreateError(validationResult.ErrorMessage);
            }

            try
            {
                // Ensure the upload directory exists
                if (!Directory.Exists(_uploadDirectory))
                {
                    Directory.CreateDirectory(_uploadDirectory);
                }

                // Create a unique filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(_uploadDirectory, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return FileUploadResult.CreateSuccess(filePath);
            }
            catch (Exception ex)
            {
                return FileUploadResult.CreateError($"Error uploading file: {ex.Message}");
            }
        }
    }
}

