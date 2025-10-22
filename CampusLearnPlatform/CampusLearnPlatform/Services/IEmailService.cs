namespace CampusLearnPlatform.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent);
        Task<bool> SendForumReplyNotificationAsync(string recipientEmail, string recipientName, string postTitle, string replyAuthor, string replyContent, string postUrl);
        Task<bool> SendTopicReplyNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string replyAuthor, string replyContent, string topicUrl);
        Task<bool> SendTopicSubscriptionNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string subscriberName, string topicUrl);
        Task<bool> SendTopicUpdateNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string topicUrl);
    }
}

