using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("learning_material")]
    public class LearningMaterial
    {
        [Key]
        [Column("material_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; }

        [Required]
        [Column("file_path")]
        public string FilePath { get; set; }

        [Required]
        [Column("file_type")]
        public string FileType { get; set; }

        [Column("topic_id")]
        public Guid TopicId { get; set; }

        [Column("poster_id")]
        public Guid PosterId { get; set; }

        [Column("poster_type")]
        public string PosterType { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
   
        public string Description { get; set; }
        public string FileName { get; set; }

        public long FileSize { get; set; }
        public MaterialTypes MaterialType { get; set; }
    
        public int DownloadCount { get; set; }
        public bool IsPublic { get; set; }

  
   
        public int UploadedByUserId { get; set; }

 
        public virtual Topic Topic { get; set; }
        public virtual User UploadedBy { get; set; }

      
        public LearningMaterial()
        {
            UploadedAt = DateTime.Now;
            DownloadCount = 0;
            IsPublic = true;
        }

        public LearningMaterial(string title, string fileName, MaterialTypes type, Guid topicId, int uploadedBy) : this()
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
