using Microsoft.AspNetCore.Http;  // Import for IFormFile
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mes.Models.ViewModels
{
    /// <summary>
    /// View model for configurable file upload functionality
    /// </summary>
    public class FileUploadViewModel
    {
        /// <summary>
        /// The file to be uploaded
        /// </summary>
        [Required(ErrorMessage = "Please select a file.")]
        [Display(Name = "Scegli un file:")]
        public IFormFile? FileToUpload { get; set; }

        #region File Type Configuration

        /// <summary>
        /// List of allowed file extensions (e.g., ".csv", ".pdf", ".xlsx")
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new List<string>();

        /// <summary>
        /// List of allowed MIME types (e.g., "text/csv", "application/pdf")
        /// </summary>
        public List<string> AllowedMimeTypes { get; set; } = new List<string>();

        /// <summary>
        /// Maximum allowed file size in bytes
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default

        /// <summary>
        /// Friendly display of the maximum file size (e.g., "10 MB")
        /// </summary>
        [Display(Name = "Maximum file size")]
        public string MaxFileSizeDisplay { get; set; } = "10 MB";

        #endregion

        #region Form Customization

        /// <summary>
        /// Title of the upload form
        /// </summary>
        [Display(Name = "Form Title")]
        public string FormTitle { get; set; } = "Upload File";

        /// <summary>
        /// Subtitle or description text for the upload form
        /// </summary>
        [Display(Name = "Form Description")]
        public string FormDescription { get; set; } = "Select a file to upload";

        /// <summary>
        /// CSS class to apply to the form container
        /// </summary>
        public string FormCssClass { get; set; } = "file-upload-form";

        /// <summary>
        /// Whether to show the file type restrictions on the form
        /// </summary>
        public bool ShowFileTypeRestrictions { get; set; } = true;

        /// <summary>
        /// Whether to show a file preview when possible
        /// </summary>
        public bool EnableFilePreview { get; set; } = false;

        #endregion

        #region Additional Form Fields

        /// <summary>
        /// Collection of additional form fields to be submitted with the file
        /// </summary>
        public Dictionary<string, string> AdditionalFields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Flag indicating whether additional fields are required
        /// </summary>
        public bool RequireAdditionalFields { get; set; } = false;

        /// <summary>
        /// Label for comments text area if enabled
        /// </summary>
        [Display(Name = "Comments")]
        public string CommentsLabel { get; set; } = "Comments";

        /// <summary>
        /// Value for comments text area if enabled
        /// </summary>
        public string Comments { get; set; } = "";

        /// <summary>
        /// Whether to show the comments field
        /// </summary>
        public bool ShowComments { get; set; } = false;

        #endregion

        #region Button Configuration

        /// <summary>
        /// Text to display on the upload button
        /// </summary>
        [Display(Name = "Upload Button Text")]
        public string UploadButtonText { get; set; } = "Upload";

        /// <summary>
        /// CSS class for the upload button
        /// </summary>
        public string UploadButtonCssClass { get; set; } = "btn btn-primary";

        /// <summary>
        /// Text to display on the cancel button
        /// </summary>
        [Display(Name = "Cancel Button Text")]
        public string CancelButtonText { get; set; } = "Cancel";

        /// <summary>
        /// CSS class for the cancel button
        /// </summary>
        public string CancelButtonCssClass { get; set; } = "btn btn-secondary";

        /// <summary>
        /// Whether to show the cancel button
        /// </summary>
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>
        /// URL to redirect to when the cancel button is clicked
        /// </summary>
        public string CancelUrl { get; set; } = "";

        /// <summary>
        /// Whether to show a confirmation dialog before uploading
        /// </summary>
        public bool ConfirmBeforeUpload { get; set; } = false;

        /// <summary>
        /// Confirmation message to display before uploading
        /// </summary>
        public string UploadConfirmationMessage { get; set; } = "Are you sure you want to upload this file?";

        #endregion
    }
}
