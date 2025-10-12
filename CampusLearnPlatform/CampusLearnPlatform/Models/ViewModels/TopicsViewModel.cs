using System.ComponentModel.DataAnnotations;

namespace CampusLearnPlatform.Models.ViewModels
{
    public class TopicsViewModel
    {
        public string UserType { get; set; } = string.Empty; // Student or Tutor
        public string UserName { get; set; } = string.Empty;
        public List<TopicCardItem> AllTopics { get; set; } = new();
        public List<TopicCardItem> MyTopics { get; set; } = new();
        public List<TopicCardItem> SubscribedTopics { get; set; } = new();
        public List<string> AvailableModules { get; set; } = new();
        public string SelectedFilter { get; set; } = "All";
    }

    public class TopicCardItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedByType { get; set; } = string.Empty; // Student or Tutor
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public int MaterialCount { get; set; }
        public int MessageCount { get; set; }
        public string Status { get; set; } = string.Empty; // Open, In Progress, Resolved, Closed
        public string Priority { get; set; } = string.Empty; // Low, Medium, High, Urgent
        public bool IsSubscribed { get; set; }
        public bool HasUnreadMessages { get; set; }
    }

    public class CreateTopicViewModel
    {
        [Required(ErrorMessage = "Topic title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a module")]
        public string Module { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a priority level")]
        public string Priority { get; set; } = string.Empty;

        public static List<string> AvailableModules => new()
        {
            "Academic Writing",
            "Business Communication",
            "Business Management",
            "Business Intelligence",
            "Cloud-Native Application Architecture",
            "Cloud-Native Application Programming",
            "Computer Architecture",
            "Data Analytics",
            "Data Science",
            "Data Warehousing",
            "Database Administration",
            "Database Concepts",
            "Database Development",
            "End User Computing",
            "English Communication",
            "Enterprise Systems",
            "Entrepreneurship",
            "Ethics & IT Law",
            "Information Systems",
            "Innovation and Leadership",
            "Innovation Management",
            "Internet of Things (IoT)",
            "Linear Programming",
            "Machine Learning",
            "Mathematics",
            "Network Development",
            "Problem Solving",
            "Programming (General)",
            "Programming (Advanced)",
            "Programming (C#)",
            "Programming (Java)",
            "Programming (Python)",
            "Project Management",
            "Research Methods",
            "Software Analysis & Design",
            "Software Engineering",
            "Software Testing",
            "Statistics",
            "User Experience Design",
            "Web Programming"
        };

        public static List<string> PriorityLevels => new()
        {
            "Low",
            "Medium",
            "High",
            "Urgent"
        };
    }

    public class TopicDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedByType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int SubscriberCount { get; set; }
        public int ViewCount { get; set; }
        public bool IsSubscribed { get; set; }
        public bool IsCreator { get; set; }
        public List<TopicMaterialItem> Materials { get; set; } = new();
        public List<TopicMessageItem> Messages { get; set; } = new();
        public List<TopicSubscriberItem> Subscribers { get; set; } = new();
    }

    public class TopicMaterialItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public int DownloadCount { get; set; }
    }

    public class TopicMessageItem
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }

    public class TopicSubscriberItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Student or Tutor
        public DateTime SubscribedAt { get; set; }
    }
}