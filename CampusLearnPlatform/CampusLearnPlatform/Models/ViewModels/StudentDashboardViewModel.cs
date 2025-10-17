namespace CampusLearnPlatform.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int SubscribedTopics { get; set; }
        public int PendingQuestions { get; set; }
        public int ResolvedQuestions { get; set; }
        public int ActiveTutors { get; set; }
        public List<StudentActivityItem> RecentActivities { get; set; } = new();
        public List<QuestionItem> MyQuestions { get; set; } = new();
        public List<TopicItem> RecommendedTopics { get; set; } = new();
    }

    public class StudentActivityItem
    {
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // response, subscription, material, forum
    }

    public class QuestionItem
    {
        public string Id { get; set; } = string.Empty;
        public string Question { get; set; }
        public string Topic { get; set; }
        public string Status { get; set; }
        public string TimeAgo { get; set; }
        public string ReplierName { get; set; }  // NEW: Name of person who replied
        public bool IsReply { get; set; }  // NEW: Indicates if this is a reply to user's content
        public string ContentType { get; set; }  // NEW: "ForumPost" or "Topic"
        public string OriginalPostId { get; set; } = string.Empty;
    }

    public class TopicItem
    {
        public string Title { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int TutorCount { get; set; }
    }
}