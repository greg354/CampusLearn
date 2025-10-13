using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Models.Learning;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class TopicsController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<TopicsController> _logger;

        public TopicsController(CampusLearnDbContext context, ILogger<TopicsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Topics/Browse
        public async Task<IActionResult> Browse(string filter = "All", string module = "All", string search = "")
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType") ?? "Student";
                var userName = HttpContext.Session.GetString("UserName") ?? "User";

                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please login to view topics.";
                    return RedirectToAction("Login", "Account");
                }

                Guid userGuid = Guid.Parse(userId);

                // Get all topics from database
                var topicsQuery = _context.Topics.AsQueryable();

                // Apply module filter if specified
                if (!string.IsNullOrEmpty(module) && module != "All")
                {
                    var moduleEntity = await _context.Modules
                        .FirstOrDefaultAsync(m => m.ModuleName == module);

                    if (moduleEntity != null)
                    {
                        topicsQuery = topicsQuery.Where(t => t.ModuleId == moduleEntity.Id);
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    topicsQuery = topicsQuery.Where(t =>
                        t.Title.Contains(search) ||
                        t.Description.Contains(search));
                }

                var topics = await topicsQuery
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Get user's subscribed topics
                var subscribedTopicIds = await _context.Subscriptions
                    .Where(s => s.StudentId == userGuid)
                    .Select(s => s.TopicId)
                    .ToListAsync();

                // Build view model
                var allTopics = new List<TopicCardItem>();
                var myTopics = new List<TopicCardItem>();
                var subscribedTopics = new List<TopicCardItem>();

                foreach (var topic in topics)
                {
                    var topicCard = await BuildTopicCardItem(topic, subscribedTopicIds, userGuid, userType);

                    allTopics.Add(topicCard);

                    // Add to my topics if user created it
                    if ((userType == "Student" && topic.StudentCreatorId == userGuid) ||
                        (userType == "Tutor" && topic.TutorCreatorId == userGuid))
                    {
                        myTopics.Add(topicCard);
                    }

                    // Add to subscribed topics
                    if (topicCard.IsSubscribed)
                    {
                        subscribedTopics.Add(topicCard);
                    }
                }

                var viewModel = new TopicsViewModel
                {
                    UserType = userType,
                    UserName = userName,
                    AvailableModules = CreateTopicViewModel.AvailableModules,
                    AllTopics = allTopics,
                    MyTopics = myTopics,
                    SubscribedTopics = subscribedTopics,
                    SelectedFilter = filter
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading topics browse page");
                TempData["ErrorMessage"] = "Unable to load topics. Please try again.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Topics/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType") ?? "Student";

                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "Please login to view topic details.";
                    return RedirectToAction("Login", "Account");
                }

                Guid userGuid = Guid.Parse(userId);

                // Get topic from database
                var topic = await _context.Topics.FindAsync(id);

                if (topic == null)
                {
                    TempData["ErrorMessage"] = "Topic not found.";
                    return RedirectToAction("Browse");
                }

                // Increment view count
                topic.IncrementViewCount();
                await _context.SaveChangesAsync();

                // Get module name
                var module = await _context.Modules.FindAsync(topic.ModuleId);
                var moduleName = module?.ModuleName ?? "Unknown Module";

                // Get creator information
                string creatorName = "Unknown";
                string creatorType = "Student";

                if (topic.StudentCreatorId.HasValue)
                {
                    var student = await _context.Students.FindAsync(topic.StudentCreatorId.Value);
                    creatorName = student?.Name ?? "Unknown Student";
                    creatorType = "Student";
                }
                else if (topic.TutorCreatorId.HasValue)
                {
                    var tutor = await _context.Tutors.FindAsync(topic.TutorCreatorId.Value);
                    creatorName = tutor?.Name ?? "Unknown Tutor";
                    creatorType = "Tutor";
                }

                // Check if user is subscribed
                var isSubscribed = await _context.Subscriptions
                    .AnyAsync(s => s.StudentId == userGuid && s.TopicId == id);

                // Check if user is creator
                var isCreator = (userType == "Student" && topic.StudentCreatorId == userGuid) ||
                               (userType == "Tutor" && topic.TutorCreatorId == userGuid);

                // Get subscriber count
                var subscriberCount = await _context.Subscriptions
                    .CountAsync(s => s.TopicId == id);

                // Get materials
                var materials = await GetTopicMaterials(id);

                // Get messages (replies)
                var messages = await GetTopicMessages(id);

                // Get subscribers
                var subscribers = await GetTopicSubscribers(id);

                var viewModel = new TopicDetailsViewModel
                {
                    Id = topic.Id,
                    Title = topic.Title,
                    Description = topic.Description ?? "",
                    Module = moduleName,
                    CreatedBy = creatorName,
                    CreatedByType = creatorType,
                    CreatedAt = topic.CreatedAt,
                    Status = topic.Status.ToString(),
                    Priority = topic.Priority.ToString(),
                    SubscriberCount = subscriberCount,
                    ViewCount = topic.ViewCount,
                    IsSubscribed = isSubscribed,
                    IsCreator = isCreator,
                    Materials = materials,
                    Messages = messages,
                    Subscribers = subscribers
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading topic details for {TopicId}", id);
                TempData["ErrorMessage"] = "Unable to load topic details. Please try again.";
                return RedirectToAction("Browse");
            }
        }

        // GET: Topics/Create
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please login to create a topic.";
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // POST: Topics/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTopicViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to create a topic.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                // Find or create module
                var module = await _context.Modules
                    .FirstOrDefaultAsync(m => m.ModuleName == model.Module);

                if (module == null)
                {
                    module = new Module
                    {
                        Id = Guid.NewGuid(),
                        ModuleName = model.Module,
                        Description = $"Module for {model.Module}"
                    };
                    _context.Modules.Add(module);
                    await _context.SaveChangesAsync();
                }

                // Parse priority
                var priority = Enums.Priorities.Medium;
                Enum.TryParse(model.Priority, out priority);

                // Create topic
                var topic = new Topic
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    Description = model.Description,
                    ModuleId = module.Id,
                    CreatedAt = DateTime.UtcNow,
                    Priority = priority,
                    Status = Enums.TopicStatuses.Open,
                    ViewCount = 0,
                    IsArchived = false
                };

                // Set creator based on user type
                if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                {
                    topic.StudentCreatorId = userId;
                }
                else if (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    topic.TutorCreatorId = userId;
                }
                else
                {
                    topic.StudentCreatorId = userId; // Default to student
                }

                _context.Topics.Add(topic);
                await _context.SaveChangesAsync();

                // Auto-subscribe creator to the topic
                var subscription = new Subscriptions
                {
                    Id = Guid.NewGuid(),
                    StudentId = userId,
                    TopicId = topic.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Topic created: {TopicId} by {UserId}", topic.Id, userId);
                TempData["SuccessMessage"] = "Topic created successfully! Tutors have been notified.";

                return RedirectToAction("Details", new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating topic");
                ModelState.AddModelError("", "Unable to create topic. Please try again.");
                return View(model);
            }
        }

        // POST: Topics/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(Guid topicId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to subscribe.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                // Check if already subscribed
                var existingSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.StudentId == userId && s.TopicId == topicId);

                if (existingSubscription != null)
                {
                    TempData["InfoMessage"] = "You are already subscribed to this topic.";
                    return RedirectToAction("Details", new { id = topicId });
                }

                // Create subscription
                var subscription = new Subscriptions
                {
                    Id = Guid.NewGuid(),
                    StudentId = userId,
                    TopicId = topicId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} subscribed to topic {TopicId}", userId, topicId);
                TempData["SuccessMessage"] = "Successfully subscribed to topic!";

                return RedirectToAction("Details", new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "Unable to subscribe. Please try again.";
                return RedirectToAction("Details", new { id = topicId });
            }
        }

        // POST: Topics/Unsubscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unsubscribe(Guid topicId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to unsubscribe.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.StudentId == userId && s.TopicId == topicId);

                if (subscription == null)
                {
                    TempData["InfoMessage"] = "You are not subscribed to this topic.";
                    return RedirectToAction("Details", new { id = topicId });
                }

                _context.Subscriptions.Remove(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} unsubscribed from topic {TopicId}", userId, topicId);
                TempData["SuccessMessage"] = "Successfully unsubscribed from topic.";

                return RedirectToAction("Details", new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "Unable to unsubscribe. Please try again.";
                return RedirectToAction("Details", new { id = topicId });
            }
        }

        // POST: Topics/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(Guid topicId, string content, bool isAnonymous = false)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to reply.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Reply content cannot be empty.";
                return RedirectToAction("Details", new { id = topicId });
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                var reply = new TopicReply
                {
                    ReplyId = Guid.NewGuid(),
                    TopicId = topicId,
                    ReplyContent = content,
                    IsAnonymous = isAnonymous,
                    CreatedAt = DateTime.UtcNow
                };

                // Set poster based on user type
                if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                {
                    reply.StudentPosterId = userId;
                }
                else if (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    reply.TutorPosterId = userId;
                }
                else
                {
                    reply.StudentPosterId = userId; // Default to student
                }

                _context.TopicReplies.Add(reply);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reply created: {ReplyId} to topic {TopicId}", reply.ReplyId, topicId);
                TempData["SuccessMessage"] = "Your reply has been posted!";

                return RedirectToAction("Details", new { id = topicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply to topic {TopicId}", topicId);
                TempData["ErrorMessage"] = "Unable to post reply. Please try again.";
                return RedirectToAction("Details", new { id = topicId });
            }
        }

        // Helper Methods

        private async Task<TopicCardItem> BuildTopicCardItem(
            Topic topic,
            List<Guid> subscribedTopicIds,
            Guid currentUserId,
            string userType)
        {
            // Get module name
            var module = await _context.Modules.FindAsync(topic.ModuleId);
            var moduleName = module?.ModuleName ?? "Unknown Module";

            // Get creator information
            string creatorName = "Unknown";
            string creatorType = "Student";

            if (topic.StudentCreatorId.HasValue)
            {
                var student = await _context.Students.FindAsync(topic.StudentCreatorId.Value);
                creatorName = student?.Name ?? "Unknown Student";

                // Check if it's current user
                if (topic.StudentCreatorId.Value == currentUserId)
                {
                    creatorName = "You";
                }
                creatorType = "Student";
            }
            else if (topic.TutorCreatorId.HasValue)
            {
                var tutor = await _context.Tutors.FindAsync(topic.TutorCreatorId.Value);
                creatorName = tutor?.Name ?? "Unknown Tutor";

                // Check if it's current user
                if (topic.TutorCreatorId.Value == currentUserId)
                {
                    creatorName = "You";
                }
                creatorType = "Tutor";
            }

            // Get counts
            var subscriberCount = await _context.Subscriptions
                .CountAsync(s => s.TopicId == topic.Id);

            var materialCount = await _context.LearningMaterials
                .CountAsync(m => m.TopicId == topic.Id);

            var messageCount = await _context.TopicReplies
                .CountAsync(r => r.TopicId == topic.Id);

            // Check if subscribed
            var isSubscribed = subscribedTopicIds.Contains(topic.Id);

            // Check for unread messages (simplified - just check if there are new messages)
            var hasUnreadMessages = messageCount > 0;

            return new TopicCardItem
            {
                Id = topic.Id,
                Title = topic.Title,
                Description = topic.Description ?? "",
                Module = moduleName,
                CreatedBy = creatorName,
                CreatedByType = creatorType,
                CreatedAt = topic.CreatedAt,
                TimeAgo = GetTimeAgo(topic.CreatedAt),
                SubscriberCount = subscriberCount,
                MaterialCount = materialCount,
                MessageCount = messageCount,
                Status = topic.Status.ToString(),
                Priority = topic.Priority.ToString(),
                IsSubscribed = isSubscribed,
                HasUnreadMessages = hasUnreadMessages
            };
        }

        private async Task<List<TopicMaterialItem>> GetTopicMaterials(Guid topicId)
        {
            var materials = await _context.LearningMaterials
                .Where(m => m.TopicId == topicId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

            var materialItems = new List<TopicMaterialItem>();

            foreach (var material in materials)
            {
                string uploaderName = "Unknown";

                if (material.StudentPosterId.HasValue)
                {
                    var student = await _context.Students.FindAsync(material.StudentPosterId.Value);
                    uploaderName = student?.Name ?? "Unknown Student";
                }
                else if (material.TutorPosterId.HasValue)
                {
                    var tutor = await _context.Tutors.FindAsync(material.TutorPosterId.Value);
                    uploaderName = tutor?.Name ?? "Unknown Tutor";
                }

                materialItems.Add(new TopicMaterialItem
                {
                    Id = material.Id,
                    Title = material.Title,
                    FileName = material.FilePath,
                    FileType = material.FileType,
                    FileSize = FormatFileSize(material.FileSize),
                    UploadedBy = uploaderName,
                    UploadedAt = material.UploadedAt,
                    TimeAgo = GetTimeAgo(material.UploadedAt),
                    DownloadCount = material.DownloadCount
                });
            }

            return materialItems;
        }

        private async Task<List<TopicMessageItem>> GetTopicMessages(Guid topicId)
        {
            var replies = await _context.TopicReplies
                .Where(r => r.TopicId == topicId)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var messageItems = new List<TopicMessageItem>();

            foreach (var reply in replies)
            {
                string senderName = "Unknown";
                string senderType = "Student";

                if (reply.IsAnonymous)
                {
                    var posterId = reply.StudentPosterId ?? reply.TutorPosterId ?? Guid.Empty;
                    senderName = GetCodename(posterId);
                }
                else
                {
                    if (reply.StudentPosterId.HasValue)
                    {
                        var student = await _context.Students.FindAsync(reply.StudentPosterId.Value);
                        senderName = student?.Name ?? "Unknown Student";
                        senderType = "Student";
                    }
                    else if (reply.TutorPosterId.HasValue)
                    {
                        var tutor = await _context.Tutors.FindAsync(reply.TutorPosterId.Value);
                        senderName = tutor?.Name ?? "Unknown Tutor";
                        senderType = "Tutor";
                    }
                }

                messageItems.Add(new TopicMessageItem
                {
                    Id = reply.ReplyId,
                    Content = reply.ReplyContent,
                    SenderName = senderName,
                    SenderType = senderType,
                    SentAt = reply.CreatedAt,
                    TimeAgo = GetTimeAgo(reply.CreatedAt),
                    IsRead = true // Simplified for now
                });
            }

            return messageItems;
        }

        private async Task<List<TopicSubscriberItem>> GetTopicSubscribers(Guid topicId)
        {
            var subscriptions = await _context.Subscriptions
                .Where(s => s.TopicId == topicId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var subscriberItems = new List<TopicSubscriberItem>();

            foreach (var subscription in subscriptions)
            {
                var student = await _context.Students.FindAsync(subscription.StudentId);

                if (student != null)
                {
                    subscriberItems.Add(new TopicSubscriberItem
                    {
                        Id = student.Id,
                        Name = student.Name,
                        Type = "Student",
                        SubscribedAt = subscription.CreatedAt
                    });
                }
            }

            return subscriberItems;
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

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

        private string GetCodename(Guid userId)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(userId.ToByteArray());
                var hashInt = BitConverter.ToInt32(hash, 0);
                var index = Math.Abs(hashInt % Codename.Adjectives.Count);
                var index2 = Math.Abs(hashInt % Codename.Nouns.Count);

                return $"{Codename.Adjectives[index]} {Codename.Nouns[index2]}";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    // Codename generator for anonymous users
    public static class Codenames
    {
        public static List<string> Adjectives = new List<string>
        {
            "Swift", "Clever", "Bold", "Bright", "Calm", "Daring", "Eager", "Fearless",
            "Gentle", "Happy", "Intelligent", "Jovial", "Keen", "Lively", "Mighty", "Noble",
            "Quick", "Radiant", "Strong", "Thoughtful", "Vibrant", "Wise", "Zealous"
        };

        public static List<string> Nouns = new List<string>
        {
            "Eagle", "Falcon", "Hawk", "Phoenix", "Dragon", "Tiger", "Lion", "Panther",
            "Wolf", "Bear", "Fox", "Owl", "Raven", "Shark", "Dolphin", "Orca",
            "Cheetah", "Leopard", "Jaguar", "Cobra", "Python", "Viper", "Lynx"
        };
    }
}