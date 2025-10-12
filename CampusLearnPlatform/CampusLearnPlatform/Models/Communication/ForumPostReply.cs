using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("forum_post_reply")]
    public class ForumPostReply
    {
        [Key]
        [Column("reply_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid ReplyId { get; set; }

        [Required]
        [Column("post_id")]
        public Guid PostId { get; set; }

        [Column("student_poster_id")]
        public Guid? StudentPosterId { get; set; }

        [Column("tutor_poster_id")]
        public Guid? TutorPosterId { get; set; }

        [Required]
        [Column("reply_content")]
        public string ReplyContent { get; set; }

        [Column("is_anonymous")]
        public bool IsAnonymous { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
