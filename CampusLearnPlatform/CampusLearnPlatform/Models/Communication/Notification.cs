using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Users;

namespace CampusLearnPlatform.Models.Communication
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public NotificationTypes NotificationType { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string ActionUrl { get; set; }

    
        public int UserId { get; set; }


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
