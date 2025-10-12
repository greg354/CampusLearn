using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Controllers
{
    public class DashboardController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(CampusLearnDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string role)
        {
            var userID = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserType") ?? role.ToLower();

            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(userRole))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!Guid.TryParse(userID, out Guid userGuid))
            {
                _logger.LogError("Invalid user ID in session: {UserId}", userID);
                return RedirectToAction("Login", "Account");
            }

            if (userRole.Equals("Tutor", StringComparison.OrdinalIgnoreCase))
            {
                return await TutorDashboard(userGuid);
            }
            else
            {
                return await StudentDashboard(userGuid);
            }
        }

        private async Task<IActionResult> StudentDashboard(Guid studentId)
        {
            try
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    _logger.LogWarning("Student not found: {StudentId}", studentId);
                    return RedirectToAction("Login", "Account");
                }

                var subscribedTopicsCount = await _context.Subscriptions
                    .Where(s => s.StudentId == studentId)
                    .CountAsync();

                var pendingQuestionsCount = await _context.ForumPosts
                    .Where(fp => fp.StudentAuthorId == studentId)
                    .CountAsync();

                var resolvedQuestionsCount = await _context.ForumPosts
                    .Where(fp => fp.StudentAuthorId == studentId)
                    .CountAsync() / 2;

                var activeTutorsCount = await _context.Messages
                    .Where(m => m.StudentReceiverId == studentId && m.TutorSenderId != null)
                    .Select(m => m.TutorSenderId)
                    .Distinct()
                    .CountAsync();

                var recentActivities = await GetStudentRecentActivities(studentId);
                var myQuestions = await GetStudentQuestions(studentId);
                var recommendedTopics = await GetRecommendedTopics(studentId);

                var viewModel = new StudentDashboardViewModel
                {
                    UserName = student.Name ?? "Student",
                    SubscribedTopics = subscribedTopicsCount,
                    PendingQuestions = pendingQuestionsCount,
                    ResolvedQuestions = resolvedQuestionsCount,
                    ActiveTutors = activeTutorsCount,
                    RecentActivities = recentActivities,
                    MyQuestions = myQuestions,
                    RecommendedTopics = recommendedTopics
                };

                return View("StudentDashboard", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading student dashboard for {StudentId}", studentId);
                TempData["ErrorMessage"] = "Error loading dashboard. Please try again.";
                return RedirectToAction("Login", "Account");
            }
        }

        private async Task<IActionResult> TutorDashboard(Guid tutorId)
        {
            try
            {
                var tutor = await _context.Tutors
                    .FirstOrDefaultAsync(t => t.Id == tutorId);

                if (tutor == null)
                {
                    _logger.LogWarning("Tutor not found: {TutorId}", tutorId);
                    return RedirectToAction("Login", "Account");
                }

                var studentsHelpedCount = await _context.Messages
                    .Where(m => m.TutorSenderId == tutorId && m.StudentReceiverId != null)
                    .Select(m => m.StudentReceiverId)
                    .Distinct()
                    .CountAsync();

                var tutorTopicIds = await _context.Topics
                    .Where(t => t.TutorCreatorId == tutorId)
                    .Select(t => t.Id)
                    .ToListAsync();

                var pendingQueriesCount = tutorTopicIds.Any()
                    ? await _context.ForumPosts
                        .Where(fp => fp.StudentAuthorId != null)
                        .CountAsync()
                    : 0;

                var topicsManagedCount = tutorTopicIds.Count;

                var responseRate = studentsHelpedCount > 0 ? 92 : 0;

                var recentActivities = await GetTutorRecentActivities(tutorId);
                var questionsRequiringResponse = await GetQuestionsRequiringResponse(tutorId);
                var myTopics = await GetTutorTopics(tutorId);

                var viewModel = new TutorDashboardViewModel
                {
                    UserName = tutor.Name ?? "Tutor",
                    StudentsHelped = studentsHelpedCount,
                    PendingQueries = pendingQueriesCount,
                    TopicsManaged = topicsManagedCount,
                    ResponseRate = responseRate,
                    RecentActivities = recentActivities,
                    QuestionsRequiringResponse = questionsRequiringResponse,
                    MyTopics = myTopics
                };

                return View("TutorDashboard", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tutor dashboard for {TutorId}", tutorId);
                TempData["ErrorMessage"] = "Error loading dashboard. Please try again.";
                return RedirectToAction("Login", "Account");
            }
        }

        private async Task<List<StudentActivityItem>> GetStudentRecentActivities(Guid studentId)
        {
            var activities = new List<StudentActivityItem>();

            var recentPosts = await _context.ForumPosts
                .Where(fp => fp.StudentAuthorId == studentId)
                .OrderByDescending(fp => fp.CreatedAt)
                .Take(2)
                .ToListAsync();

            foreach (var post in recentPosts)
            {
                activities.Add(new StudentActivityItem
                {
                    Description = "You posted a question in the forum",
                    TimeAgo = GetTimeAgo(post.CreatedAt),
                    Icon = "fas fa-comment",
                    Type = "response"
                });
            }

            var recentSubscriptions = await _context.Subscriptions
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.CreatedAt)
                .Take(2)
                .ToListAsync();

            foreach (var sub in recentSubscriptions)
            {
                var topic = await _context.Topics.FindAsync(sub.TopicId);

                activities.Add(new StudentActivityItem
                {
                    Description = $"Subscribed to {topic?.Title ?? "a topic"}",
                    TimeAgo = GetTimeAgo(sub.CreatedAt),
                    Icon = "fas fa-book",
                    Type = "subscription"
                });
            }

            return activities.OrderByDescending(a => a.TimeAgo).Take(4).ToList();
        }

        private async Task<List<QuestionItem>> GetStudentQuestions(Guid studentId)
        {
            var questions = await _context.ForumPosts
                .Where(fp => fp.StudentAuthorId == studentId)
                .OrderByDescending(fp => fp.CreatedAt)
                .Take(3)
                .ToListAsync();

            return questions.Select(q => new QuestionItem
            {
                Question = q.PostContent?.Length > 50
                    ? q.PostContent.Substring(0, 50) + "..."
                    : q.PostContent ?? "No content",
                Topic = "General",
                Status = "Pending",
                TimeAgo = GetTimeAgo(q.CreatedAt)
            }).ToList();
        }

        private async Task<List<TopicItem>> GetRecommendedTopics(Guid studentId)
        {
            var subscribedTopicIds = await _context.Subscriptions
                .Where(s => s.StudentId == studentId)
                .Select(s => s.TopicId)
                .ToListAsync();

            var recommendedTopics = await _context.Topics
                .Where(t => !subscribedTopicIds.Contains(t.Id))
                .Take(3)
                .ToListAsync();

            var topicItems = new List<TopicItem>();

            foreach (var topic in recommendedTopics)
            {
                var studentCount = await _context.Subscriptions
                    .Where(s => s.TopicId == topic.Id)
                    .CountAsync();

                var tutorCount = await _context.Topics
                    .Where(t => t.Id == topic.Id && t.TutorCreatorId != null)
                    .CountAsync();

                topicItems.Add(new TopicItem
                {
                    Title = topic.Title,
                    Module = "General",
                    StudentCount = studentCount,
                    TutorCount = tutorCount
                });
            }
            return topicItems;
        }

        private async Task<List<TutorActivityItem>> GetTutorRecentActivities(Guid tutorId)
        {
            var activities = new List<TutorActivityItem>();

            var recentMessages = await _context.Messages
                .Where(m => m.TutorSenderId == tutorId && m.StudentReceiverId != null)
                .OrderByDescending(m => m.Timestamp)
                .Take(4)
                .ToListAsync();

            foreach (var message in recentMessages)
            {
                activities.Add(new TutorActivityItem
                {
                    Description = "Responded to a student query",
                    TimeAgo = GetTimeAgo(message.Timestamp),
                    Icon = "fas fa-reply",
                    Type = "question",
                    Priority = "Normal"
                });
            }

            return activities;
        }

        private async Task<List<QuestionToAnswerItem>> GetQuestionsRequiringResponse(Guid tutorId)
        {
            var tutorTopicIds = await _context.Topics
                .Where(t => t.TutorCreatorId == tutorId)
                .Select(t => t.Id)
                .ToListAsync();

            var questions = await _context.ForumPosts
                .Where(fp => fp.StudentAuthorId != null)
                .OrderByDescending(fp => fp.CreatedAt)
                .Take(3)
                .ToListAsync();

            var questionItems = new List<QuestionToAnswerItem>();

            foreach (var question in questions)
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Id == question.StudentAuthorId);

                questionItems.Add(new QuestionToAnswerItem
                {
                    Question = question.PostContent?.Length > 60
                        ? question.PostContent.Substring(0, 60) + "..."
                        : question.PostContent ?? "No content",
                    Topic = "General",
                    StudentName = student?.Name ?? "Unknown Student",
                    TimeAgo = GetTimeAgo(question.CreatedAt),
                    Priority = "Normal"
                });
            }

            return questionItems;
        }

        private async Task<List<TutorTopicItem>> GetTutorTopics(Guid tutorId)
        {
            var tutorTopics = await _context.Topics
                .Where(t => t.TutorCreatorId == tutorId)
                .Take(3)
                .ToListAsync();

            var topicItems = new List<TutorTopicItem>();

            foreach (var topic in tutorTopics)
            {
                var subscriberCount = await _context.Subscriptions
                    .Where(s => s.TopicId == topic.Id)
                    .CountAsync();

                var pendingQuestions = await _context.ForumPosts
                    .Where(fp => fp.StudentAuthorId != null)
                    .CountAsync();

                topicItems.Add(new TutorTopicItem
                {
                    Title = topic.Title,
                    Module = "General",
                    SubscriberCount = subscriberCount,
                    PendingQuestions = pendingQuestions
                });
            }

            return topicItems;
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago";
        }

        public IActionResult SwitchRole(string role)
        {
            HttpContext.Session.SetString("UserType", role.ToLower());
            return RedirectToAction("Index");
        }
    }
}