using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Users;
using System.Reflection;

namespace CampusLearnPlatform.Models.Learning
{
    public class Topic
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public TopicStatuses Status { get; set; }
        public int ViewCount { get; set; }
        public Priorities Priority { get; set; }
        public bool IsArchived { get; set; }

        // Foreign Keys
        public int StudentId { get; set; }
        public int ModuleId { get; set; }

        // Navigation Properties
        public virtual Student CreatedBy { get; set; }
        public virtual Module Module { get; set; }
        public virtual ICollection<Subscription> Subscriptions { get; set; }
        public virtual ICollection<LearningMaterial> Materials { get; set; }
        public virtual ICollection<PrivateMessage> Messages { get; set; }

       
        public Topic()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            Status = TopicStatus.Open;
            ViewCount = 0;
            Priority = Priority.Medium;
            IsArchived = false;
            Subscriptions = new List<Subscription>();
            Materials = new List<LearningMaterial>();
            Messages = new List<PrivateMessage>();
        }

        public Topic(string title, string description, int studentId, int moduleId) : this()
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
        public List<Subscription> GetSubscribers()
        {
            return new List<Subscription>();
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
