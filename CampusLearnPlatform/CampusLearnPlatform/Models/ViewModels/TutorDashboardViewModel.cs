namespace CampusLearnPlatform.Models.ViewModels
{
    public class TutorDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int StudentsHelped { get; set; }
        public int PendingQueries { get; set; }
        public int TopicsManaged { get; set; }
        public int ResponseRate { get; set; } // Percentage
        public List<TutorActivityItem> RecentActivities { get; set; } = new();
        public List<QuestionToAnswerItem> QuestionsRequiringResponse { get; set; } = new();
        public List<TutorTopicItem> MyTopics { get; set; } = new();
    }

    public class TutorActivityItem
    {
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // question, subscription, material, forum
        public string Priority { get; set; } = string.Empty; // Normal, Urgent
    }

    public class QuestionToAnswerItem
    {
        public string Question { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty; // Normal, Urgent
    }

    public class TutorTopicItem
    {
        public string Title { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public int PendingQuestions { get; set; }
    }
}