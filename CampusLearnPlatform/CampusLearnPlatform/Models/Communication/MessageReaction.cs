using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_reaction")]
    public class MessageReaction
    {
        [Key]
        [Column("reaction_id")]
        public Guid Id { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("emoji")]
        public string Emoji { get; set; } = default!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PrivateMessage? Message { get; set; }
    }
}
