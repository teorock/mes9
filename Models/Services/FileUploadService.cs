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

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace mes.Models.Services
{
    /// <summary>
    /// Interface for file upload service that handles validation and saving of uploaded files
    /// </summary>
    public interface IFileUploadService
    {
        /// <summary>
        /// Validates if the file is valid based on size and allowed extensions
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <param name="maxSizeInMB">Maximum allowed size in MB</param>
        /// <param name="allowedExtensions">Array of allowed file extensions</param>
        /// <returns>Tuple with validation result and error message if any</returns>
        (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file, int maxSizeInMB = 10, string[] allowedExtensions = null);

        /// <summary>
        /// Uploads a file to the configured directory
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <param name="useUniqueFileName">Whether to prepend a GUID to the filename</param>
        /// <returns>Result with file path and success status</returns>
        Task<FileUploadResult> UploadFileAsync(IFormFile file, bool useUniqueFileName = true);
    }

    /// <summary>
    /// Result object for file upload operations
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
        public string FileName { get; set; }
    }

    /// <summary>
    /// Service that handles file upload operations with validation and proper error handling
    /// </summary>
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadDirectory;

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Get upload directory from configuration or use default
            _uploadDirectory = _configuration["FileUpload:UploadDirectory"] ?? "c:\\temp\\uploads";
        }

        /// <summary>
        /// Validates if the file is valid based on size and allowed extensions
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file, int maxSizeInMB = 10, string[] allowedExtensions = null)
        {
            // Check if file is null
            if (file == null || file.Length == 0)
            {
                return (false, "No file was selected for upload.");
            }

            // Check file size
            if (file.Length > maxSizeInMB * 1024 * 1024)
            {
                return (false, $"File size exceeds the maximum allowed size of {maxSizeInMB} MB.");
            }

            // Check file extension if allowedExtensions is provided
            if (allowedExtensions != null && allowedExtensions.Length > 0)
            {
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (string.IsNullOrEmpty(fileExtension) || !Array.Exists(allowedExtensions, ext => ext.ToLowerInvariant() == fileExtension))
                {
                    return (false, $"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Uploads a file to the configured directory
        /// </summary>
        public async Task<FileUploadResult> UploadFileAsync(IFormFile file, bool useUniqueFileName = true)
        {
            try
            {
                // Validate file
                var (isValid, errorMessage) = ValidateFile(file);
                if (!isValid)
                {
                    return new FileUploadResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    };
                }

                // Ensure upload directory exists
                if (!Directory.Exists(_uploadDirectory))
                {
                    Directory.CreateDirectory(_uploadDirectory);
                    _logger.LogInformation($"Created directory: {_uploadDirectory}");
                }

                // Generate file name (with GUID prefix if useUniqueFileName is true)
                string fileName = file.FileName;
                if (useUniqueFileName)
                {
                    fileName = $"{Guid.NewGuid()}_{fileName}";
                }

                // Combine directory and file name to get full path
                string filePath = Path.Combine(_uploadDirectory, fileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File uploaded successfully: {filePath}");

                return new FileUploadResult
                {
                    Success = true,
                    FilePath = filePath,
                    FileName = fileName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Error uploading file: {ex.Message}"
                };
            }
        }
    }
}

