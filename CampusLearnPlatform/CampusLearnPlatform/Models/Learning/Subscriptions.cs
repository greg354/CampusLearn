using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("subscription")]
    public class Subscriptions
    {
        [Key]
        [Column("subscription_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [Column("topic_id")]
        public Guid TopicId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        public DateTime SubscribedAt { get; set; }
        public bool IsActive { get; set; }
        public bool ReceiveNotifications { get; set; }

        public virtual Student Student { get; set; }
        public virtual Topic Topic { get; set; }

     
        public Subscriptions()
        {
            SubscribedAt = DateTime.Now;
            IsActive = true;
            ReceiveNotifications = true;
        }

        public Subscriptions(Guid studentId, Guid topicId) : this()
        {
            StudentId = studentId;
            TopicId = topicId;
        }

        public void Activate()
        {
            IsActive = true;
        }
        public void Deactivate()
        {
            IsActive = false;
        }
        public void ToggleNotifications()
        {
            ReceiveNotifications = !ReceiveNotifications;
        }
        public bool ShouldNotify()
        {
            return IsActive && ReceiveNotifications;
        }
        public TimeSpan GetSubscriptionDuration()
        {
            return DateTime.Now.Subtract(SubscribedAt);
        }
    }
}
