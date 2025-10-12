using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("forum_post")]
    public class ForumPosts
    {
        [Key]
        [Column("post_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("post_content")]
        public string PostContent { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("student_author_id")]
        public Guid? StudentAuthorId { get; set; }

        [Column("tutor_author_id")]
        public Guid? TutorAuthorId { get; set; }

        [Column("is_anonymous")]
        public bool IsAnonymous { get; set; }

        [Column("upvote_count")]
        public int UpvoteCount { get; set; }

        [Column("downvote_count")]
        public int DownvoteCount { get; set; }

        // Computed properties (not in DB)
        [NotMapped]
        public Guid AuthorId => StudentAuthorId ?? TutorAuthorId ?? Guid.Empty;

        [NotMapped]
        public string AuthorType => StudentAuthorId.HasValue ? "Student" :
                                     TutorAuthorId.HasValue ? "Tutor" : "Unknown";

        public ForumPosts()
        {
            CreatedAt = DateTime.UtcNow;
            UpvoteCount = 0;
            DownvoteCount = 0;
            IsAnonymous = false;
        }

        public void Upvote()
        {
            UpvoteCount++;
        }

        public void Downvote()
        {
            DownvoteCount++;
        }

        public int GetNetVotes()
        {
            return UpvoteCount - DownvoteCount;
        }
    }
}