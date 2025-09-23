using Microsoft.AspNetCore.Mvc;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            // Simple mock data for frontend
            var viewModel = new DashboardViewModel
            {
                UserName = "John Doe",
                SubscribedTopics = 8,
                TutorResponses = 15,
                CompletedSessions = 4,
                ActiveDiscussions = 3,
                RecentActivities = new List<ActivityItem>
                {
                    new ActivityItem { Description = "New response to your Data Structures question", TimeAgo = "2 hours ago", Icon = "fas fa-comment" },
                    new ActivityItem { Description = "Subscribed to Advanced Algorithms", TimeAgo = "1 day ago", Icon = "fas fa-book" },
                    new ActivityItem { Description = "Downloaded Java Programming Notes", TimeAgo = "2 days ago", Icon = "fas fa-download" },
                    new ActivityItem { Description = "Completed tutoring session in Database Management", TimeAgo = "3 days ago", Icon = "fas fa-check-circle" }
                }
            };

            return View(viewModel);
        }
    }
}