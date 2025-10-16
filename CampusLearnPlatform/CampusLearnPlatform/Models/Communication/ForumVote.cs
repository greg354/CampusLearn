using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("forum_vote")]
    public class ForumVote
    {
        [Key]
        [Column("vote_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid VoteId { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("user_type")]
        [StringLength(20)]
        public string UserType { get; set; } = string.Empty; // "Student" or "Tutor"

        [Required]
        [Column("target_id")]
        public Guid TargetId { get; set; } // PostId or ReplyId

        [Required]
        [Column("target_type")]
        [StringLength(20)]
        public string TargetType { get; set; } = string.Empty; // "Post" or "Reply"

        [Required]
        [Column("vote_type")]
        [StringLength(10)]
        public string VoteType { get; set; } = string.Empty; // "Upvote" or "Downvote"

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}