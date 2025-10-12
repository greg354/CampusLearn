using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Users;
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

        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime PostedAt { get; set; }
        public bool IsAnonymous { get; set; }
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public bool IsModerated { get; set; }
        public bool IsApproved { get; set; }
        public string ModerationNotes { get; set; }

        public int PostedById { get; set; }
        public int ModuleId { get; set; }
        public int? ParentPostId { get; set; }
        public Guid TopicId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorType { get; set; }

        public virtual User PostedBy { get; set; }
        public virtual Module Module { get; set; }
        public virtual ForumPosts ParentPost { get; set; }
        public virtual ICollection<ForumPosts> Replies { get; set; }

        public ForumPosts()
        {
            PostedAt = DateTime.Now;
            UpvoteCount = 0;
            DownvoteCount = 0;
            IsModerated = false;
            IsApproved = false;
            Replies = new List<ForumPosts>();
        }

        public ForumPosts(string title, string content, int postedById, int moduleId, bool isAnonymous) : this()
        {
            Title = title;
            Content = content;
            PostedById = postedById;
            ModuleId = moduleId;
            IsAnonymous = isAnonymous;
        }

        public void AddReply(ForumPosts reply) { }

        public void Upvote()
        {
            UpvoteCount++;
        }

        public void Downvote()
        {
            DownvoteCount++;
        }

        public void Moderate(bool approve, string notes)
        {
            IsModerated = true;
            IsApproved = approve;
            ModerationNotes = notes;
        }

        public int GetNetVotes()
        {
            return UpvoteCount - DownvoteCount;
        }

        public bool IsReply()
        {
            return ParentPostId.HasValue;
        }

        public void UpdateContent(string newContent)
        {
            Content = newContent;
        }
    }
}