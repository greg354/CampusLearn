using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("topic")]
    public class Topic
    {
        [Key]
        [Column("topic_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("module_id")]
        public Guid ModuleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
      
        public DateTime UpdatedAt { get; set; }
        public TopicStatuses Status { get; set; }
        public int ViewCount { get; set; }
        public Priorities Priority { get; set; }
        public bool IsArchived { get; set; }

        public int StudentId { get; set; }
       

        public virtual Student CreatedBy { get; set; }
        public virtual Module Module { get; set; }
        public virtual ICollection<Subscriptions> Subscriptions { get; set; }
        public virtual ICollection<LearningMaterial> Materials { get; set; }
        public virtual ICollection<PrivateMessage> Messages { get; set; }

        public Topic()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Status = TopicStatuses.Open;
            ViewCount = 0;
            Priority = Priorities.Medium;
            IsArchived = false;
            Subscriptions = new List<Subscriptions>();
            Materials = new List<LearningMaterial>();
            Messages = new List<PrivateMessage>();
        }

        public Topic(string title, string description, int studentId, Guid moduleId) : this()
        {
            Title = title;
            Description = description;
            StudentId = studentId;
            ModuleId = moduleId;
        }

       
        public void UpdateStatus(TopicStatuses newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.Now;
        }
        public void AddMaterial(LearningMaterial material) { }
        public void IncrementViewCount()
        {
            ViewCount++;
        }
        public List<Subscriptions> GetSubscribers()
        {
            return new List<Subscriptions>();
        }
        public void NotifySubscribers(string message) { }
        public bool IsActiveForTutor(int tutorId)
        {
            return Status == TopicStatuses.Open && !IsArchived;
        }
        public void CloseTopic()
        {
            Status = TopicStatuses.Closed;
            UpdatedAt = DateTime.Now;
        }
        public void ArchiveTopic()
        {
            IsArchived = true;
            UpdatedAt = DateTime.Now;
        }
    }
}
