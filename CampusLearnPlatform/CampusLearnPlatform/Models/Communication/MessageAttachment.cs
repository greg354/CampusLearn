using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_attachment")]
    public class MessageAttachment
    {
        [Key]
        [Column("attachment_id")]
        public Guid Id { get; set; }

        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("file_name")]
        public string FileName { get; set; } = default!;

        [Column("content_type")]
        public string ContentType { get; set; } = default!;

        [Column("file_size_bytes")]
        public long FileSizeBytes { get; set; }

        [Column("storage_path")]
        public string StoragePath { get; set; } = default!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public PrivateMessage? Message { get; set; }
    }
}
