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

        [Column("sent_at")]
        public DateTime Timestamp { get; set; }

        // Sender variants
        [Column("student_sender_id")]
        public Guid? StudentSenderId { get; set; }

        [Column("tutor_sender_id")]
        public Guid? TutorSenderId { get; set; }

        [Column("admin_sender_id")]
        public Guid? AdminSenderId { get; set; }

        // Receiver variants
        [Column("student_receiver_id")]
        public Guid? StudentReceiverId { get; set; }

        [Column("tutor_receiver_id")]
        public Guid? TutorReceiverId { get; set; }

        [Column("admin_receiver_id")]
        public Guid? AdminReceiverId { get; set; }

        // ---- convenience / legacy fields (not in DB)
        [NotMapped] public string Content { get; set; }
        [NotMapped] public DateTime SentAt { get; set; }

        // ---- flags NOT present as columns in your DB — mark as NotMapped so EF won't SELECT them
        [NotMapped] public bool IsRead { get; set; }            // would map to 'is_read' if you ever add it
        [NotMapped] public MessageStatuses Status { get; set; } // would map to 'status'
        [NotMapped] public DateTime? ReadAt { get; set; }       // would map to 'read_at'
        [NotMapped] public bool IsDeleted { get; set; }         // would map to 'is_deleted'

        // Unused relational extras (kept for compatibility)
        [NotMapped] public int? TopicId { get; set; }
        [NotMapped] public int? ParentMessageId { get; set; }
        [NotMapped] public Guid SenderId { get; set; }
        [NotMapped] public string SenderType { get; set; }
        [NotMapped] public Guid ReceiverId { get; set; }
        [NotMapped] public string ReceiverType { get; set; }
        [NotMapped] public virtual User Sender { get; set; }
        [NotMapped] public virtual User Receiver { get; set; }
        [NotMapped] public virtual Topic Topic { get; set; }
        [NotMapped] public virtual PrivateMessage ParentMessage { get; set; }

        public PrivateMessage()
        {
            Timestamp = DateTime.UtcNow;
            Status = MessageStatuses.Sent;
            IsRead = false;
            IsDeleted = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
            ReadAt = DateTime.Now;
            Status = MessageStatuses.Read;
        }
    }
}
