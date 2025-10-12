using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("notification")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("student_recipient_id")]
        public Guid? StudentRecipientId { get; set; }

        [Column("tutor_recipient_id")]
        public Guid? TutorRecipientId { get; set; }

        [Column("admin_recipient_id")]
        public Guid? AdminRecipientId { get; set; }

        public string Title { get; set; }
        public bool IsRead { get; set; }
        public NotificationTypes NotificationType { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string ActionUrl { get; set; }
        public int UserId { get; set; }
        public Guid RecipientId { get; set; }
        public string RecipientType { get; set; }

        public virtual User User { get; set; }

        public Notification()
        {
            CreatedAt = DateTime.Now;
            IsRead = false;
            IsSent = false;
        }

        public Notification(int userId, string title, string message, NotificationTypes type) : this()
        {
            UserId = userId;
            Title = title;
            Message = message;
            NotificationType = type;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.Now;
        }
        public void Send()
        {
            IsSent = true;
            SentAt = DateTime.Now;
        }
        public bool ShouldSend()
        {
            return !IsSent && CreatedAt > DateTime.Now.AddDays(-7);
        }
        public void SetActionUrl(string url)
        {
            ActionUrl = url;
        }
        public string GetFormattedMessage()
        {
            return $"{Title}: {Message}";
        }
        public bool IsExpired()
        {
            return CreatedAt < DateTime.Now.AddDays(-30);
        }
    }
}
