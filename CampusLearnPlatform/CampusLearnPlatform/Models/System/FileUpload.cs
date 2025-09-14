using CampusLearnPlatform.Models.Users;

namespace CampusLearnPlatform.Models.System
{
    public class FileUpload
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsDeleted { get; set; }
        public string FileHash { get; set; }

        public int UploadedByUserId { get; set; }

        public virtual User UploadedBy { get; set; }

        public FileUpload()
        {
            UploadedAt = DateTime.Now;
            IsDeleted = false;
        }

        public FileUpload(string originalFileName, string storedFileName, long fileSize, string contentType, int uploadedBy) : this()
        {
            OriginalFileName = originalFileName;
            StoredFileName = storedFileName;
            FileSize = fileSize;
            ContentType = contentType;
            UploadedByUserId = uploadedBy;
        }

        public bool ValidateFile()
        {
            return !string.IsNullOrEmpty(OriginalFileName) && FileSize > 0;
        }
        public void DeleteFile()
        {
            IsDeleted = true;
        }
        public string GetFileUrl()
        {
            return $"/uploads/{StoredFileName}";
        }
        public bool IsImageFile()
        {
            return ContentType?.StartsWith("image/") == true;
        }
        public bool IsPDFFile()
        {
            return ContentType == "application/pdf";
        }
        public double GetFileSizeInMB()
        {
            return FileSize / (1024.0 * 1024.0);
        }
        public void UpdateFilePath(string newPath)
        {
            FilePath = newPath;
        }
    }
}
