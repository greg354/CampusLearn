using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_reply")]
    public class MessageReply
    {
        [Key]
        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("parent_message_id")]
        public Guid ParentMessageId { get; set; }

        public PrivateMessage? Message { get; set; }
    }
}
