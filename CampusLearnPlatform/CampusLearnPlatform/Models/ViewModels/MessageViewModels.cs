using CampusLearnPlatform.Models.Communication;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<MessageViewModel> Messages { get; set; } = new List<MessageViewModel>();
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

    public class MessageViewModel
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = "";
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public bool IsSentByCurrentUser { get; set; }
        public string Status { get; set; } = "";

        public string GetTimestamp()
        {
            var messageTime = Timestamp;

            // If the timestamp is stored as UTC, convert to local time
            if (Timestamp.Kind == DateTimeKind.Utc)
            {
                messageTime = Timestamp.ToLocalTime();
            }

            var timeAgo = DateTime.Now - messageTime;

            if (timeAgo.TotalSeconds < 60)
                return "Just now";
            if (timeAgo.TotalMinutes < 1)
                return "Just now";
            if (timeAgo.TotalHours < 1)
                return $"{(int)timeAgo.TotalMinutes}m ago";
            if (timeAgo.TotalDays < 1)
                return $"{(int)timeAgo.TotalHours}h ago";
            if (timeAgo.TotalDays < 7)
                return $"{(int)timeAgo.TotalDays}d ago";

            return messageTime.ToString("MMM dd, yyyy 'at' h:mm tt");
        }

    }

    public class ComposeMessageViewModel
    {
        public string? SearchQuery { get; set; }
        public NewMessageViewModel NewMessage { get; set; } = new NewMessageViewModel();
        public List<MessageViewModel>? ExistingMessages { get; set; }
        public bool HasExistingConversation => ExistingMessages?.Any() == true;
        public string? SelectedUserId { get; set; }
        public string? SelectedUserName { get; set; }
    }
}