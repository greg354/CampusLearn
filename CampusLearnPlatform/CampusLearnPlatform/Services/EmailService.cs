using SendGrid;
using SendGrid.Helpers.Mail;

namespace CampusLearnPlatform.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _apiKey = configuration["SendGrid:ApiKey"] ?? throw new ArgumentNullException("SendGrid API key not configured");
            _fromEmail = configuration["SendGrid:FromEmail"] ?? throw new ArgumentNullException("SendGrid FromEmail not configured");
            _fromName = configuration["SendGrid:FromName"] ?? "CampusLearn";
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, toName);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

                var response = await client.SendEmailAsync(msg);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                    return true;
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send email to {ToEmail}. Status: {StatusCode}, Body: {Body}",
                        toEmail, response.StatusCode, body);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendForumReplyNotificationAsync(string recipientEmail, string recipientName, string postTitle, string replyAuthor, string replyContent, string postUrl)
        {
            var subject = $"New Reply on Your Forum Post: {postTitle}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height:1.6; color:#333; max-width:600px; margin:0 auto; padding:20px; }}
.header {{ background:linear-gradient(135deg,#6366f1 0%,#8b5cf6 100%); color:white; padding:30px; text-align:center; border-radius:10px 10px 0 0; }}
.header h1 {{ margin:0; font-size:24px; }}
.content {{ background:#fff; padding:30px; border:1px solid #e0e0e0; border-top:none; }}
.reply-box {{ background:#f8f9fa; border-left:4px solid #6366f1; padding:15px; margin:20px 0; border-radius:5px; }}
.reply-author {{ font-weight:bold; color:#6366f1; margin-bottom:10px; }}
.button {{ display:inline-block; background:linear-gradient(135deg,#6366f1 0%,#8b5cf6 100%); color:white; padding:12px 30px; text-decoration:none; border-radius:5px; margin:20px 0; }}
.footer {{ background:#f8f9fa; padding:20px; text-align:center; font-size:12px; color:#6c757d; border-radius:0 0 10px 10px; }}
</style>
</head>
<body>
<div class='header'>
<h1>CampusLearn™</h1>
<p>You have a new reply!</p>
</div>
<div class='content'>
<h2>Hi {recipientName},</h2>
<p><strong>{replyAuthor}</strong> replied to your forum post:</p>
<p><strong>""{postTitle}""</strong></p>
<div class='reply-box'>
<div class='reply-author'> {replyAuthor} said:</div>
<p>{replyContent}</p>
</div>
<p>Click the button below to view the full discussion and reply:</p>
<a href='{postUrl}' class='button'>View Full Discussion</a>
<p style='margin-top:30px; font-size:14px; color:#6c757d;'>This is an automated notification from CampusLearn. You received this because you posted this question on the forum.</p>
</div>
<div class='footer'>
<p><strong>CampusLearn™</strong></p>
<p>Empowering Student Success Through Peer-Powered Learning</p>
<p>&copy; 2024 Belgium Campus. All rights reserved.</p>
</div>
</body>
</html>";
            return await SendEmailAsync(recipientEmail, recipientName, subject, htmlContent);
        }

        public async Task<bool> SendTopicReplyNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string replyAuthor, string replyContent, string topicUrl)
        {
            var subject = $"New Reply in Topic: {topicTitle}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{ font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height:1.6; color:#333; max-width:600px; margin:0 auto; padding:20px; }}
.header {{ background:linear-gradient(135deg,#28a745 0%,#20c997 100%); color:white; padding:30px; text-align:center; border-radius:10px 10px 0 0; }}
.header h1 {{ margin:0; font-size:24px; }}
.content {{ background:#fff; padding:30px; border:1px solid #e0e0e0; border-top:none; }}
.reply-box {{ background:#f8f9fa; border-left:4px solid #28a745; padding:15px; margin:20px 0; border-radius:5px; }}
.reply-author {{ font-weight:bold; color:#28a745; margin-bottom:10px; }}
.button {{ display:inline-block; background:linear-gradient(135deg,#28a745 0%,#20c997 100%); color:white; padding:12px 30px; text-decoration:none; border-radius:5px; margin:20px 0; }}
.footer {{ background:#f8f9fa; padding:20px; text-align:center; font-size:12px; color:#6c757d; border-radius:0 0 10px 10px; }}
</style>
</head>
<body>
<div class='header'>
<h1>CampusLearn™</h1>
<p>New message in your topic!</p>
</div>
<div class='content'>
<h2>Hi {recipientName},</h2>
<p><strong>{replyAuthor}</strong> posted a message in your topic:</p>
<p><strong> ""{topicTitle}""</strong></p>
<div class='reply-box'>
<div class='reply-author'> {replyAuthor} said:</div>
<p>{replyContent}</p>
</div>
<p>Click the button below to view the full conversation and respond:</p>
<a href='{topicUrl}' class='button'>View Topic</a>
<p style='margin-top:30px; font-size:14px; color:#6c757d;'>This is an automated notification from CampusLearn. You received this because you created this topic.</p>
</div>
<div class='footer'>
<p><strong>CampusLearn™</strong></p>
<p>Empowering Student Success Through Peer-Powered Learning</p>
<p>&copy; 2024 Belgium Campus. All rights reserved.</p>
</div>
</body>
</html>";
            return await SendEmailAsync(recipientEmail, recipientName, subject, htmlContent);
        }

        public async Task<bool> SendTopicSubscriptionNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string subscriberName, string topicUrl)
        {
            var subject = $"New Subscriber to Your Topic: {topicTitle}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{ font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height:1.6; color:#333; max-width:600px; margin:0 auto; padding:20px; }}
.header {{ background:linear-gradient(135deg,#ffc107 0%,#ff9800 100%); color:white; padding:30px; text-align:center; border-radius:10px 10px 0 0; }}
.header h1 {{ margin:0; font-size:24px; }}
.content {{ background:#fff; padding:30px; border:1px solid #e0e0e0; border-top:none; }}
.subscriber-box {{ background:#fff9e6; border-left:4px solid #ffc107; padding:15px; margin:20px 0; border-radius:5px; }}
.button {{ display:inline-block; background:linear-gradient(135deg,#ffc107 0%,#ff9800 100%); color:white; padding:12px 30px; text-decoration:none; border-radius:5px; margin:20px 0; }}
.footer {{ background:#f8f9fa; padding:20px; text-align:center; font-size:12px; color:#6c757d; border-radius:0 0 10px 10px; }}
</style>
</head>
<body>
<div class='header'>
<h1> CampusLearn™</h1>
<p>Your topic is growing!</p>
</div>
<div class='content'>
<h2>Hi {recipientName},</h2>
<p>Great news! <strong>{subscriberName}</strong> just subscribed to your topic:</p>
<p><strong> ""{topicTitle}""</strong></p>
<div class='subscriber-box'>
<p> <strong>{subscriberName}</strong> is now following your topic and will receive updates when new messages or materials are posted.</p>
</div>
<p>Your topic is helping students learn! Keep the conversation going:</p>
<a href='{topicUrl}' class='button'>View Topic</a>
<p style='margin-top:30px; font-size:14px; color:#6c757d;'>This is an automated notification from CampusLearn. You received this because you created this topic.</p>
</div>
<div class='footer'>
<p><strong>CampusLearn™</strong></p>
<p>Empowering Student Success Through Peer-Powered Learning</p>
<p>&copy; 2024 Belgium Campus. All rights reserved.</p>
</div>
</body>
</html>";
            return await SendEmailAsync(recipientEmail, recipientName, subject, htmlContent);
        }

        public async Task<bool> SendTopicUpdateNotificationAsync(string recipientEmail, string recipientName, string topicTitle, string topicUrl)
        {
            var subject = $"Topic Updated: {topicTitle}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{ font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height:1.6; color:#333; max-width:600px; margin:0 auto; padding:20px; }}
.header {{ background:linear-gradient(135deg,#007bff 0%,#0056b3 100%); color:white; padding:30px; text-align:center; border-radius:10px 10px 0 0; }}
.header h1 {{ margin:0; font-size:24px; }}
.content {{ background:#fff; padding:30px; border:1px solid #e0e0e0; border-top:none; }}
.update-box {{ background:#e7f3ff; border-left:4px solid #007bff; padding:15px; margin:20px 0; border-radius:5px; }}
.button {{ display:inline-block; background:linear-gradient(135deg,#007bff 0%,#0056b3 100%); color:white; padding:12px 30px; text-decoration:none; border-radius:5px; margin:20px 0; }}
.footer {{ background:#f8f9fa; padding:20px; text-align:center; font-size:12px; color:#6c757d; border-radius:0 0 10px 10px; }}
</style>
</head>
<body>
<div class='header'>
<h1> CampusLearn™</h1>
<p>A topic you're following has been updated!</p>
</div>
<div class='content'>
<h2>Hi {recipientName},</h2>
<p>The topic you're subscribed to has been updated:</p>
<p><strong> ""{topicTitle}""</strong></p>
<div class='update-box'>
<p> The topic creator has made changes to the topic details. Check it out to see what's new!</p>
</div>
<p>Click the button below to view the updated topic:</p>
<a href='{topicUrl}' class='button'>View Updated Topic</a>
<p style='margin-top:30px; font-size:14px; color:#6c757d;'>This is an automated notification from CampusLearn. You received this because you subscribed to this topic.</p>
</div>
<div class='footer'>
<p><strong>CampusLearn™</strong></p>
<p>Empowering Student Success Through Peer-Powered Learning</p>
<p>&copy; 2024 Belgium Campus. All rights reserved.</p>
</div>
</body>
</html>";
            return await SendEmailAsync(recipientEmail, recipientName, subject, htmlContent);
        }
    }
}