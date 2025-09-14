using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Users;

namespace CampusLearnPlatform.Models.Learning
{
    public class LearningMaterial
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public MaterialTypes MaterialType { get; set; }
        public DateTime UploadedAt { get; set; }
        public int DownloadCount { get; set; }
        public bool IsPublic { get; set; }

  
        public int TopicId { get; set; }
        public int UploadedByUserId { get; set; }

 
        public virtual Topic Topic { get; set; }
        public virtual User UploadedBy { get; set; }

      
        public LearningMaterial()
        {
            UploadedAt = DateTime.Now;
            DownloadCount = 0;
            IsPublic = true;
        }

        public LearningMaterial(string title, string fileName, MaterialTypes type, int topicId, int uploadedBy) : this()
        {
            Title = title;
            FileName = fileName;
            MaterialType = type;
            TopicId = topicId;
            UploadedByUserId = uploadedBy;
        }

    
        public bool ValidateFile()
        {
            return !string.IsNullOrEmpty(FileName) && FileSize > 0;
        }
        public string GetDownloadUrl()
        {
            return $"/downloads/{FileName}";
        }
        public void IncrementDownloadCount()
        {
            DownloadCount++;
        }
        public bool CanUserAccess(int userId)
        {
            return IsPublic || UploadedByUserId == userId;
        }
        public void UpdateDescription(string newDescription)
        {
            Description = newDescription;
        }
        public double GetFileSizeInMB()
        {
            return FileSize / (1024.0 * 1024.0);
        }
    }
}
