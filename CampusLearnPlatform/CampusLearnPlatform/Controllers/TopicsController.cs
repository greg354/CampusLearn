using Microsoft.AspNetCore.Mvc;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Controllers
{
    public class TopicsController : Controller
    {
        public IActionResult Browse(string filter = "All")
        {
            // Get user type from session
            var userType = HttpContext.Session.GetString("UserType") ?? "Student";
            var userName = HttpContext.Session.GetString("UserName") ?? "Test User";

            var viewModel = new TopicsViewModel
            {
                UserType = userType,
                UserName = userName,
                SelectedFilter = filter,
                AvailableModules = CreateTopicViewModel.AvailableModules,
                AllTopics = GetMockTopics(userType),
                MyTopics = GetMockMyTopics(userType),
                SubscribedTopics = GetMockSubscribedTopics()
            };

            return View(viewModel);
        }

        // Redirect Index to Browse for backwards compatibility
        public IActionResult Index(string filter = "All")
        {
            return RedirectToAction("Browse", new { filter });
        }

        public IActionResult Details(Guid id)
        {
            var userType = HttpContext.Session.GetString("UserType") ?? "Student";
            var userName = HttpContext.Session.GetString("UserName") ?? "Test User";

            var viewModel = new TopicDetailsViewModel
            {
                Id = id,
                Title = "Understanding Binary Search Trees",
                Description = "I need help understanding how to implement and traverse binary search trees in Java. Specifically looking for explanations on in-order, pre-order, and post-order traversal methods.",
                Module = "Data Structures",
                CreatedBy = "Sarah Williams",
                CreatedByType = "Student",
                CreatedAt = DateTime.Now.AddDays(-2),
                Status = "Open",
                Priority = "High",
                SubscriberCount = 8,
                ViewCount = 45,
                IsSubscribed = true,
                IsCreator = userName == "Sarah Williams",
                Materials = GetMockMaterials(),
                Messages = GetMockMessages(),
                Subscribers = GetMockSubscribers()
            };

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(CreateTopicViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // In a real implementation, this would save to database
            TempData["SuccessMessage"] = "Topic created successfully! Tutors have been notified.";
            return RedirectToAction("Browse");
        }

        [HttpPost]
        public IActionResult Subscribe(Guid topicId)
        {
            // Mock subscription
            TempData["SuccessMessage"] = "Successfully subscribed to topic!";
            return RedirectToAction("Details", new { id = topicId });
        }

        [HttpPost]
        public IActionResult Unsubscribe(Guid topicId)
        {
            // Mock unsubscription
            TempData["SuccessMessage"] = "Successfully unsubscribed from topic.";
            return RedirectToAction("Details", new { id = topicId });
        }

        // Mock data methods
        private List<TopicCardItem> GetMockTopics(string userType)
        {
            return new List<TopicCardItem>
            {
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Understanding Binary Search Trees",
                    Description = "Need help with BST implementation and traversal methods in Java.",
                    Module = "Data Structures",
                    CreatedBy = "Sarah Williams",
                    CreatedByType = "Student",
                    CreatedAt = DateTime.Now.AddHours(-2),
                    TimeAgo = "2 hours ago",
                    SubscriberCount = 8,
                    MaterialCount = 3,
                    MessageCount = 12,
                    Status = "Open",
                    Priority = "High",
                    IsSubscribed = false,
                    HasUnreadMessages = true
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "SQL JOIN Operations Explained",
                    Description = "Looking for clear explanations and examples of INNER, LEFT, RIGHT, and OUTER joins.",
                    Module = "Database Concepts",
                    CreatedBy = "Michael Brown",
                    CreatedByType = "Student",
                    CreatedAt = DateTime.Now.AddHours(-5),
                    TimeAgo = "5 hours ago",
                    SubscriberCount = 15,
                    MaterialCount = 5,
                    MessageCount = 28,
                    Status = "In Progress",
                    Priority = "Medium",
                    IsSubscribed = true,
                    HasUnreadMessages = false
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "RESTful API Design Best Practices",
                    Description = "Discussion on REST principles, HTTP methods, status codes, and API versioning.",
                    Module = "Web Programming",
                    CreatedBy = "Dr. Jane Smith",
                    CreatedByType = "Tutor",
                    CreatedAt = DateTime.Now.AddDays(-1),
                    TimeAgo = "1 day ago",
                    SubscriberCount = 22,
                    MaterialCount = 8,
                    MessageCount = 45,
                    Status = "Open",
                    Priority = "Medium",
                    IsSubscribed = true,
                    HasUnreadMessages = true
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Machine Learning Fundamentals",
                    Description = "Introduction to supervised and unsupervised learning with Python examples.",
                    Module = "Machine Learning",
                    CreatedBy = "Emily Davis",
                    CreatedByType = "Student",
                    CreatedAt = DateTime.Now.AddDays(-2),
                    TimeAgo = "2 days ago",
                    SubscriberCount = 18,
                    MaterialCount = 10,
                    MessageCount = 67,
                    Status = "Open",
                    Priority = "Low",
                    IsSubscribed = false,
                    HasUnreadMessages = false
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Object-Oriented Programming Principles",
                    Description = "Deep dive into encapsulation, inheritance, polymorphism, and abstraction.",
                    Module = "Programming (Java)",
                    CreatedBy = "Prof. John Anderson",
                    CreatedByType = "Tutor",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    TimeAgo = "3 days ago",
                    SubscriberCount = 32,
                    MaterialCount = 12,
                    MessageCount = 89,
                    Status = "Open",
                    Priority = "High",
                    IsSubscribed = true,
                    HasUnreadMessages = false
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Cloud Architecture Patterns",
                    Description = "Understanding microservices, serverless, and container orchestration.",
                    Module = "Cloud-Native Application Architecture",
                    CreatedBy = "David Wilson",
                    CreatedByType = "Student",
                    CreatedAt = DateTime.Now.AddDays(-4),
                    TimeAgo = "4 days ago",
                    SubscriberCount = 12,
                    MaterialCount = 6,
                    MessageCount = 34,
                    Status = "Resolved",
                    Priority = "Medium",
                    IsSubscribed = false,
                    HasUnreadMessages = false
                }
            };
        }

        private List<TopicCardItem> GetMockMyTopics(string userType)
        {
            if (userType == "Student")
            {
                return new List<TopicCardItem>
                {
                    new TopicCardItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Understanding Binary Search Trees",
                        Description = "Need help with BST implementation and traversal methods in Java.",
                        Module = "Data Structures",
                        CreatedBy = "You",
                        CreatedByType = "Student",
                        CreatedAt = DateTime.Now.AddHours(-2),
                        TimeAgo = "2 hours ago",
                        SubscriberCount = 8,
                        MaterialCount = 3,
                        MessageCount = 12,
                        Status = "Open",
                        Priority = "High",
                        IsSubscribed = true,
                        HasUnreadMessages = true
                    }
                };
            }
            else
            {
                return new List<TopicCardItem>
                {
                    new TopicCardItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "RESTful API Design Best Practices",
                        Description = "Discussion on REST principles, HTTP methods, status codes, and API versioning.",
                        Module = "Web Programming",
                        CreatedBy = "You",
                        CreatedByType = "Tutor",
                        CreatedAt = DateTime.Now.AddDays(-1),
                        TimeAgo = "1 day ago",
                        SubscriberCount = 22,
                        MaterialCount = 8,
                        MessageCount = 45,
                        Status = "Open",
                        Priority = "Medium",
                        IsSubscribed = true,
                        HasUnreadMessages = true
                    }
                };
            }
        }

        private List<TopicCardItem> GetMockSubscribedTopics()
        {
            return new List<TopicCardItem>
            {
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "SQL JOIN Operations Explained",
                    Description = "Looking for clear explanations and examples of joins.",
                    Module = "Database Concepts",
                    CreatedBy = "Michael Brown",
                    CreatedByType = "Student",
                    CreatedAt = DateTime.Now.AddHours(-5),
                    TimeAgo = "5 hours ago",
                    SubscriberCount = 15,
                    MaterialCount = 5,
                    MessageCount = 28,
                    Status = "In Progress",
                    Priority = "Medium",
                    IsSubscribed = true,
                    HasUnreadMessages = false
                },
                new TopicCardItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Object-Oriented Programming Principles",
                    Description = "Deep dive into OOP concepts.",
                    Module = "Programming (Java)",
                    CreatedBy = "Prof. John Anderson",
                    CreatedByType = "Tutor",
                    CreatedAt = DateTime.Now.AddDays(-3),
                    TimeAgo = "3 days ago",
                    SubscriberCount = 32,
                    MaterialCount = 12,
                    MessageCount = 89,
                    Status = "Open",
                    Priority = "High",
                    IsSubscribed = true,
                    HasUnreadMessages = false
                }
            };
        }

        private List<TopicMaterialItem> GetMockMaterials()
        {
            return new List<TopicMaterialItem>
            {
                new TopicMaterialItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Binary Search Tree Implementation Guide",
                    FileName = "bst_implementation.pdf",
                    FileType = "PDF",
                    FileSize = "2.4 MB",
                    UploadedBy = "Dr. Jane Smith",
                    UploadedAt = DateTime.Now.AddHours(-1),
                    TimeAgo = "1 hour ago",
                    DownloadCount = 12
                },
                new TopicMaterialItem
                {
                    Id = Guid.NewGuid(),
                    Title = "BST Traversal Examples",
                    FileName = "bst_traversal.java",
                    FileType = "Code",
                    FileSize = "15 KB",
                    UploadedBy = "Prof. John Anderson",
                    UploadedAt = DateTime.Now.AddHours(-3),
                    TimeAgo = "3 hours ago",
                    DownloadCount = 8
                },
                new TopicMaterialItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Video Tutorial: Binary Trees Explained",
                    FileName = "https://youtu.be/example",
                    FileType = "Video",
                    FileSize = "N/A",
                    UploadedBy = "Dr. Jane Smith",
                    UploadedAt = DateTime.Now.AddHours(-5),
                    TimeAgo = "5 hours ago",
                    DownloadCount = 24
                }
            };
        }

        private List<TopicMessageItem> GetMockMessages()
        {
            return new List<TopicMessageItem>
            {
                new TopicMessageItem
                {
                    Id = Guid.NewGuid(),
                    Content = "I've uploaded a comprehensive guide on BST implementation. Check the materials section!",
                    SenderName = "Dr. Jane Smith",
                    SenderType = "Tutor",
                    SentAt = DateTime.Now.AddMinutes(-30),
                    TimeAgo = "30 minutes ago",
                    IsRead = true
                },
                new TopicMessageItem
                {
                    Id = Guid.NewGuid(),
                    Content = "Thank you! The guide is really helpful. Could you also explain the time complexity of different operations?",
                    SenderName = "Sarah Williams",
                    SenderType = "Student",
                    SentAt = DateTime.Now.AddMinutes(-15),
                    TimeAgo = "15 minutes ago",
                    IsRead = true
                },
                new TopicMessageItem
                {
                    Id = Guid.NewGuid(),
                    Content = "Sure! For a balanced BST: Search, Insert, Delete are all O(log n). I'll add a complexity analysis document shortly.",
                    SenderName = "Dr. Jane Smith",
                    SenderType = "Tutor",
                    SentAt = DateTime.Now.AddMinutes(-5),
                    TimeAgo = "5 minutes ago",
                    IsRead = false
                }
            };
        }

        private List<TopicSubscriberItem> GetMockSubscribers()
        {
            return new List<TopicSubscriberItem>
            {
                new TopicSubscriberItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Dr. Jane Smith",
                    Type = "Tutor",
                    SubscribedAt = DateTime.Now.AddHours(-2)
                },
                new TopicSubscriberItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Michael Brown",
                    Type = "Student",
                    SubscribedAt = DateTime.Now.AddHours(-1)
                },
                new TopicSubscriberItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Emily Davis",
                    Type = "Student",
                    SubscribedAt = DateTime.Now.AddMinutes(-45)
                }
            };
        }
    }
}