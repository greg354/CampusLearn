using CampusLearnPlatform.Models.Users;
using System;
using System.Collections.Generic;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.Learning;

namespace CampusLearnPlatform.Models.Communication
{
    public class ForumPosts
    {
        public int Id { get; set; }
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
