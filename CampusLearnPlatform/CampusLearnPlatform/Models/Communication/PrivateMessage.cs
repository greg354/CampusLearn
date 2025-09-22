using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message")]
    public class PrivateMessage
    {
        [Key]
        [Column("message_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("message_content")]
        public string MessageContent { get; set; }

        [Column("sender_type")]
        public string SenderType { get; set; }

        [Column("sender_id")]
        public Guid SenderId { get; set; }

        [Column("receiver_type")]
        public string ReceiverType { get; set; }

        [Column("receiver_id")]
        public Guid ReceiverId { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; }
    
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public MessageStatuses Status { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsDeleted { get; set; }


      
        public int? TopicId { get; set; }
        public int? ParentMessageId { get; set; }

        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
        public virtual Topic Topic { get; set; }
        public virtual PrivateMessage ParentMessage { get; set; }

       
        public PrivateMessage()
        {
            SentAt = DateTime.Now;
            IsRead = false;
            Status = MessageStatuses.Sent;
            IsDeleted = false;
        }

        public PrivateMessage(Guid senderId, Guid receiverId, string content) : this()
        {
            SenderId = senderId;
            ReceiverId = receiverId;
            Content = content;
        }

   
        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.Now;
            Status = MessageStatuses.Read;
        }
        public void Reply(string replyContent, int senderId) { }
        public void Delete()
        {
            IsDeleted = true;
        }
        public bool CanUserAccess(Guid userId)
        {
            return SenderId == userId || ReceiverId == userId;
        }
        public void UpdateStatus(MessageStatuses newStatus)
        {
            Status = newStatus;
        }
        public string GetTimestamp()
        {
            return SentAt.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
