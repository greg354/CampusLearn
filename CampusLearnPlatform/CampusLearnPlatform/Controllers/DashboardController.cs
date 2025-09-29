using Microsoft.AspNetCore.Mvc;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index(string role)
        {
            // For now, we'll use a query parameter to switch between roles
            // Later, this will come from authentication/session
            // Example: /Dashboard?role=student or /Dashboard?role=tutor

            // Get role from session if exists, otherwise use parameter
            var userRole = HttpContext.Session.GetString("UserType") ?? role.ToLower();

            if (userRole == "Tutor")
            {
                return TutorDashboard();
            }
            else
            {
                return StudentDashboard();
            }
        }

        private IActionResult StudentDashboard()
        {
            // Mock data for frontend design
            var viewModel = new StudentDashboardViewModel
            {
                UserName = "John Doe",
                SubscribedTopics = 8,
                PendingQuestions = 3,
                ResolvedQuestions = 12,
                ActiveTutors = 5,
                RecentActivities = new List<StudentActivityItem>
                {
                    new StudentActivityItem
                    {
                        Description = "Your question on Data Structures was answered",
                        TimeAgo = "2 hours ago",
                        Icon = "fas fa-comment",
                        Type = "response"
                    },
                    new StudentActivityItem
                    {
                        Description = "New material uploaded in Database Management",
                        TimeAgo = "5 hours ago",
                        Icon = "fas fa-file-pdf",
                        Type = "material"
                    },
                    new StudentActivityItem
                    {
                        Description = "Subscribed to Advanced Algorithms",
                        TimeAgo = "1 day ago",
                        Icon = "fas fa-book",
                        Type = "subscription"
                    },
                    new StudentActivityItem
                    {
                        Description = "New reply in Software Engineering forum",
                        TimeAgo = "2 days ago",
                        Icon = "fas fa-comments",
                        Type = "forum"
                    }
                },
                MyQuestions = new List<QuestionItem>
                {
                    new QuestionItem
                    {
                        Question = "How do I implement a binary search tree?",
                        Topic = "Data Structures",
                        Status = "Answered",
                        TimeAgo = "2 hours ago"
                    },
                    new QuestionItem
                    {
                        Question = "Explain normalization in databases",
                        Topic = "Database Management",
                        Status = "Pending",
                        TimeAgo = "1 day ago"
                    },
                    new QuestionItem
                    {
                        Question = "What is the difference between JPA and Hibernate?",
                        Topic = "Java Programming",
                        Status = "Pending",
                        TimeAgo = "2 days ago"
                    }
                },
                RecommendedTopics = new List<TopicItem>
                {
                    new TopicItem
                    {
                        Title = "Web Development Basics",
                        Module = "Web Programming",
                        StudentCount = 45,
                        TutorCount = 3
                    },
                    new TopicItem
                    {
                        Title = "Machine Learning Fundamentals",
                        Module = "Data Science",
                        StudentCount = 32,
                        TutorCount = 2
                    },
                    new TopicItem
                    {
                        Title = "Cloud Computing with Azure",
                        Module = "Cloud Architecture",
                        StudentCount = 28,
                        TutorCount = 2
                    }
                }
            };

            return View("StudentDashboard", viewModel);
        }

        private IActionResult TutorDashboard()
        {
            // Mock data for frontend design
            var viewModel = new TutorDashboardViewModel
            {
                UserName = "Dr. Jane Smith",
                StudentsHelped = 24,
                PendingQueries = 5,
                TopicsManaged = 4,
                ResponseRate = 92,
                RecentActivities = new List<TutorActivityItem>
                {
                    new TutorActivityItem
                    {
                        Description = "New question in Database Management",
                        TimeAgo = "30 minutes ago",
                        Icon = "fas fa-question-circle",
                        Type = "question",
                        Priority = "Urgent"
                    },
                    new TutorActivityItem
                    {
                        Description = "3 students subscribed to your Data Structures topic",
                        TimeAgo = "2 hours ago",
                        Icon = "fas fa-user-plus",
                        Type = "subscription",
                        Priority = "Normal"
                    },
                    new TutorActivityItem
                    {
                        Description = "Your Java tutorial PDF was downloaded 12 times",
                        TimeAgo = "5 hours ago",
                        Icon = "fas fa-download",
                        Type = "material",
                        Priority = "Normal"
                    },
                    new TutorActivityItem
                    {
                        Description = "Question about SQL joins needs response",
                        TimeAgo = "1 day ago",
                        Icon = "fas fa-exclamation-triangle",
                        Type = "question",
                        Priority = "Urgent"
                    }
                },
                QuestionsRequiringResponse = new List<QuestionToAnswerItem>
                {
                    new QuestionToAnswerItem
                    {
                        Question = "How do I optimize complex SQL queries?",
                        Topic = "Database Management",
                        StudentName = "Sarah Williams",
                        TimeAgo = "30 minutes ago",
                        Priority = "Urgent"
                    },
                    new QuestionToAnswerItem
                    {
                        Question = "Explain the difference between composition and inheritance",
                        Topic = "Object-Oriented Programming",
                        StudentName = "Michael Brown",
                        TimeAgo = "3 hours ago",
                        Priority = "Normal"
                    },
                    new QuestionToAnswerItem
                    {
                        Question = "Best practices for RESTful API design?",
                        Topic = "Web Development",
                        StudentName = "Emily Davis",
                        TimeAgo = "1 day ago",
                        Priority = "Urgent"
                    }
                },
                MyTopics = new List<TutorTopicItem>
                {
                    new TutorTopicItem
                    {
                        Title = "Database Management Fundamentals",
                        Module = "Database Concepts",
                        SubscriberCount = 18,
                        PendingQuestions = 2
                    },
                    new TutorTopicItem
                    {
                        Title = "Advanced Data Structures",
                        Module = "Data Structures",
                        SubscriberCount = 15,
                        PendingQuestions = 1
                    },
                    new TutorTopicItem
                    {
                        Title = "Java Programming Best Practices",
                        Module = "Programming (Java)",
                        SubscriberCount = 22,
                        PendingQuestions = 2
                    }
                }
            };

            return View("TutorDashboard", viewModel);
        }

        // Helper action to switch roles (for testing)
        public IActionResult SwitchRole(string role)
        {
            HttpContext.Session.SetString("UserType", role.ToLower());
            return RedirectToAction("Index");
        }
    }
}