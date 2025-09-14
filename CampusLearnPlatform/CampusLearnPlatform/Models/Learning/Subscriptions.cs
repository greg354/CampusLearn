using CampusLearnPlatform.Models.Users;

namespace CampusLearnPlatform.Models.Learning
{
    public class Subscriptions
    {
        public int Id { get; set; }
        public DateTime SubscribedAt { get; set; }
        public bool IsActive { get; set; }
        public bool ReceiveNotifications { get; set; }

   
        public int StudentId { get; set; }
        public int TopicId { get; set; }

  
        public virtual Student Student { get; set; }
        public virtual Topic Topic { get; set; }

     
        public Subscriptions()
        {
            SubscribedAt = DateTime.Now;
            IsActive = true;
            ReceiveNotifications = true;
        }

        public Subscriptions(int studentId, int topicId) : this()
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
