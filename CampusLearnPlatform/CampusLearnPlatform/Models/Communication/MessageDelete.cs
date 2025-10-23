using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_delete")]
    public class MessageDelete
    {
        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("deleted_at")]
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;

        public PrivateMessage? Message { get; set; }
    }
}
