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

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }

        [Column("student_poster_id")]
        public Guid? StudentPosterId { get; set; }

        [Column("tutor_poster_id")]
        public Guid? TutorPosterId { get; set; }

        [Column("admin_poster_id")]
        public Guid? AdminPosterId { get; set; }

        [NotMapped]
        public string Description { get; set; }

        [NotMapped]
        public string FileName { get; set; }

        [NotMapped]
        public long FileSize { get; set; }

        [NotMapped]
        public MaterialTypes MaterialType { get; set; }

        [NotMapped]
        public int DownloadCount { get; set; }

        [NotMapped]
        public bool IsPublic { get; set; }

        [NotMapped]
        public int UploadedByUserId { get; set; }

        [NotMapped]
        public Guid PosterId { get; set; }

        [NotMapped]
        public string PosterType { get; set; }

        [NotMapped]
        public virtual Topic Topic { get; set; }

        [NotMapped]
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
