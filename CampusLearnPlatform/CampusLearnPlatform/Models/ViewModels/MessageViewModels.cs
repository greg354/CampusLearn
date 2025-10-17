using CampusLearnPlatform.Models.Communication;
using System.ComponentModel.DataAnnotations;

namespace CampusLearnPlatform.Models.ViewModels
{
    public class MessageIndexViewModel
    {
        public List<ConversationPreview> RecentConversations { get; set; } = new List<ConversationPreview>();
        public int UnreadCount { get; set; }
    }

    public class ConversationPreview
    {
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = "";
        public string LastMessage { get; set; } = "";
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ConversationViewModel
    {
        public Guid OtherUserId { get; set; }
        public string OtherUserName { get; set; } = "";
        public string OtherUserRole { get; set; } = "";
        public List<PrivateMessage> Messages { get; set; } = new List<PrivateMessage>();
        public NewMessageViewModel NewMessage { get; set; } = new NewMessageViewModel();
    }

    public class NewMessageViewModel
    {
        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = "";
    }
}
