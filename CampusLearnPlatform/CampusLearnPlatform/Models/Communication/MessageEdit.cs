using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_edit")]
    public class MessageEdit
    {
        [Key]
        [Column("edit_id")]
        public Guid Id { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("editor_id")]
        public Guid EditorId { get; set; }

        [Column("old_content")]
        public string? OldContent { get; set; }

        [Column("new_content")]
        public string NewContent { get; set; } = default!;

        [Column("edited_at")]
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;

        public PrivateMessage? Message { get; set; }
    }
}
