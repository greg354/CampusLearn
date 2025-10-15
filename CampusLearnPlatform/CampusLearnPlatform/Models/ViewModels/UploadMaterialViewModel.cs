using System.ComponentModel.DataAnnotations;

namespace CampusLearnPlatform.Models.ViewModels
{
    public class UploadMaterialViewModel
    {
        [Required(ErrorMessage = "Please provide a title for the material")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Please select a file to upload")]
        public IFormFile File { get; set; }

        [Required]
        public Guid TopicId { get; set; }


        public string FileName => File?.FileName;
        public long FileSize => File?.Length ?? 0;
        public string FileType => GetFileType(File?.FileName);

        private string GetFileType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unknown";

            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "pdf",
                ".doc" or ".docx" => "document",
                ".ppt" or ".pptx" => "presentation",
                ".xls" or ".xlsx" => "spreadsheet",
                ".zip" or ".rar" or ".7z" => "archive",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
                ".mp4" or ".avi" or ".mov" => "video",
                ".mp3" or ".wav" => "audio",
                ".txt" => "text",
                ".cs" or ".java" or ".py" or ".js" or ".html" or ".css" => "code",
                _ => "file"
            };
        }
    }
}
