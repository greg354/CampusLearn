using System;
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

        [Column("parent_reply_id")] // NEW: For nested replies
        public Guid? ParentReplyId { get; set; }

        [Column("student_poster_id")]
        public Guid? StudentPosterId { get; set; }

        [Column("tutor_poster_id")]
        public Guid? TutorPosterId { get; set; }

        [Required]
        [Column("reply_content")]
        public string ReplyContent { get; set; } = string.Empty;

        [Column("is_anonymous")]
        public bool IsAnonymous { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("upvote_count")] // NEW
        public int UpvoteCount { get; set; } = 0;

        [Column("downvote_count")] // NEW
        public int DownvoteCount { get; set; } = 0;

        // Computed properties
        [NotMapped]
        public Guid AuthorId => StudentPosterId ?? TutorPosterId ?? Guid.Empty;

        [NotMapped]
        public string AuthorType => StudentPosterId.HasValue ? "Student" :
                                     TutorPosterId.HasValue ? "Tutor" : "Unknown";

        public void Upvote()
        {
            UpvoteCount++;
        }

        public void Downvote()
        {
            DownvoteCount++;
        }

        public void RemoveUpvote()
        {
            if (UpvoteCount > 0) UpvoteCount--;
        }

        public void RemoveDownvote()
        {
            if (DownvoteCount > 0) DownvoteCount--;
        }

        public int GetNetVotes()
        {
            return UpvoteCount - DownvoteCount;
        }
    }
}