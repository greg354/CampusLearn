namespace CampusLearnPlatform.Models.ViewModels
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int SubscribedTopics { get; set; }
        public int TutorResponses { get; set; }
        public int CompletedSessions { get; set; }
        public int ActiveDiscussions { get; set; }
        public List<ActivityItem> RecentActivities { get; set; } = new();
    }

    public class ActivityItem
    {
        public string Description { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}