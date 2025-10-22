using CampusLearnPlatform.Data;
using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class TopicsController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<TopicsController> _logger;
        private readonly IEmailService _emailService;

        public TopicsController(CampusLearnDbContext context, ILogger<TopicsController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
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
                    _logger.LogWarning("Topic not found: {TopicId}", id);
                    TempData["ErrorMessage"] = "Topic not found.";
                    return RedirectToAction("Browse");
                }

                // Increment view count
                topic.IncrementViewCount();
                await _context.SaveChangesAsync();

                // Get module name - WITH NULL CHECK
                var module = await _context.Modules.FindAsync(topic.ModuleId);
                var moduleName = module?.ModuleName ?? "Unknown Module";

                // Get creator information - WITH BETTER NULL HANDLING
                string creatorName = "Unknown";
                string creatorType = "Student";

                if (topic.StudentCreatorId.HasValue)
                {
                    var student = await _context.Students.FindAsync(topic.StudentCreatorId.Value);
                    if (student != null)
                    {
                        creatorName = student.Name ?? "Unknown Student";
                        creatorType = "Student";
                    }
                }
                else if (topic.TutorCreatorId.HasValue)
                {
                    var tutor = await _context.Tutors.FindAsync(topic.TutorCreatorId.Value);
                    if (tutor != null)
                    {
                        creatorName = tutor.Name ?? "Unknown Tutor";
                        creatorType = "Tutor";
                    }
                }

                // Check if user is subscribed
                var isSubscribed = await _context.Subscriptions
                    .AnyAsync(s => s.StudentId == userGuid && s.TopicId == id);

                // Check if user is creator
                var isCreator = (userType.Equals("Student", StringComparison.OrdinalIgnoreCase) && topic.StudentCreatorId == userGuid) ||
                               (userType.Equals("Tutor", StringComparison.OrdinalIgnoreCase) && topic.TutorCreatorId == userGuid);

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
                    Title = topic.Title ?? "Untitled Topic",
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
                    Materials = materials ?? new List<TopicMaterialItem>(),
                    Messages = messages ?? new List<TopicMessageItem>(),
                    Subscribers = subscribers ?? new List<TopicSubscriberItem>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading topic details for {TopicId}", id);
                TempData["ErrorMessage"] = $"Unable to load topic details: {ex.Message}";
                return RedirectToAction("Browse");
            }
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
                TempData["ErrorMessage"] = "Please fill in all required fields correctly.";
                return RedirectToAction("Browse");
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
                if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                {
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
                }

                _logger.LogInformation("Topic created: {TopicId} by {UserId}", topic.Id, userId);
                TempData["SuccessMessage"] = "Topic created successfully!";

                return RedirectToAction("Details", new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating topic");
                TempData["ErrorMessage"] = "Unable to create topic. Please try again.";
                return RedirectToAction("Browse");
            }
        }

        // POST: Topics/Subscribe - WITH EMAIL NOTIFICATION
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

                // ===== SEND EMAIL NOTIFICATION TO TOPIC CREATOR =====
                try
                {
                    var topic = await _context.Topics.FindAsync(topicId);
                    if (topic != null)
                    {
                        // Get topic creator's details
                        string creatorEmail = null;
                        string creatorName = null;

                        if (topic.StudentCreatorId.HasValue && topic.StudentCreatorId.Value != userId)
                        {
                            var creator = await _context.Students.FindAsync(topic.StudentCreatorId.Value);
                            if (creator != null)
                            {
                                creatorEmail = creator.Email;
                                creatorName = creator.Name;
                            }
                        }
                        else if (topic.TutorCreatorId.HasValue)
                        {
                            var creator = await _context.Tutors.FindAsync(topic.TutorCreatorId.Value);
                            if (creator != null)
                            {
                                creatorEmail = creator.Email;
                                creatorName = creator.Name;
                            }
                        }

                        // Get subscriber's name
                        var subscriber = await _context.Students.FindAsync(userId);
                        var subscriberName = subscriber?.Name ?? "A student";

                        // Send email if we have the creator's email
                        if (!string.IsNullOrEmpty(creatorEmail))
                        {
                            var topicUrl = Url.Action("Details", "Topics", new { id = topicId }, Request.Scheme);

                            await _emailService.SendTopicSubscriptionNotificationAsync(
                                creatorEmail,
                                creatorName,
                                topic.Title,
                                subscriberName,
                                topicUrl
                            );

                            _logger.LogInformation("Subscription notification sent to {Email} for topic {TopicId}",
                                creatorEmail, topicId);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send subscription notification for topic {TopicId}", topicId);
                }

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

        // POST: Topics/Reply - WITH EMAIL NOTIFICATION
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
                    reply.StudentPosterId = userId;
                }

                _context.TopicReplies.Add(reply);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reply created: {ReplyId} to topic {TopicId}", reply.ReplyId, topicId);

                // ===== SEND EMAIL NOTIFICATION TO TOPIC CREATOR =====
                try
                {
                    var topic = await _context.Topics.FindAsync(topicId);
                    if (topic != null)
                    {
                        // Get topic creator's details
                        string creatorEmail = null;
                        string creatorName = null;

                        if (topic.StudentCreatorId.HasValue && topic.StudentCreatorId.Value != userId)
                        {
                            var creator = await _context.Students.FindAsync(topic.StudentCreatorId.Value);
                            if (creator != null)
                            {
                                creatorEmail = creator.Email;
                                creatorName = creator.Name;
                            }
                        }
                        else if (topic.TutorCreatorId.HasValue && topic.TutorCreatorId.Value != userId)
                        {
                            var creator = await _context.Tutors.FindAsync(topic.TutorCreatorId.Value);
                            if (creator != null)
                            {
                                creatorEmail = creator.Email;
                                creatorName = creator.Name;
                            }
                        }

                        // Get replier's name
                        string replierName = "Anonymous User";
                        if (!isAnonymous)
                        {
                            if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                var student = await _context.Students.FindAsync(userId);
                                replierName = student?.Name ?? "A Student";
                            }
                            else if (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                var tutor = await _context.Tutors.FindAsync(userId);
                                replierName = tutor?.Name ?? "A Tutor";
                            }
                        }

                        // Send email if we have the creator's email
                        if (!string.IsNullOrEmpty(creatorEmail))
                        {
                            var topicUrl = Url.Action("Details", "Topics", new { id = topicId }, Request.Scheme);

                            // Truncate content for email (first 200 characters)
                            var contentPreview = content.Length > 200
                                ? content.Substring(0, 200) + "..."
                                : content;

                            await _emailService.SendTopicReplyNotificationAsync(
                                creatorEmail,
                                creatorName,
                                topic.Title,
                                replierName,
                                contentPreview,
                                topicUrl
                            );

                            _logger.LogInformation("Reply notification sent to {Email} for topic {TopicId}",
                                creatorEmail, topicId);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send reply notification for topic {TopicId}", topicId);
                }

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

        // POST: Topics/Update - WITH EMAIL NOTIFICATION TO SUBSCRIBERS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, string title, string description, string module, string priority)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please login to edit topics.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Title and description are required.";
                return RedirectToAction("Details", new { id = id });
            }

            try
            {
                Guid userGuid = Guid.Parse(userId);
                var topic = await _context.Topics.FindAsync(id);

                if (topic == null)
                {
                    TempData["ErrorMessage"] = "Topic not found.";
                    return RedirectToAction("Browse");
                }

                // Verify user is the creator
                bool isCreator = (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true && topic.StudentCreatorId == userGuid) ||
                                (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true && topic.TutorCreatorId == userGuid);

                if (!isCreator)
                {
                    TempData["ErrorMessage"] = "Only the topic creator can edit this topic.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Find or create module if it changed
                var moduleEntity = await _context.Modules
                    .FirstOrDefaultAsync(m => m.ModuleName == module);

                if (moduleEntity == null)
                {
                    moduleEntity = new Module
                    {
                        Id = Guid.NewGuid(),
                        ModuleName = module,
                        Description = $"Module for {module}"
                    };
                    _context.Modules.Add(moduleEntity);
                    await _context.SaveChangesAsync();
                }

                // Update topic
                topic.Title = title;
                topic.Description = description;
                topic.ModuleId = moduleEntity.Id;

                if (Enum.TryParse<Priorities>(priority, out var priorityEnum))
                {
                    topic.Priority = priorityEnum;
                }

                _context.Topics.Update(topic);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Topic {TopicId} updated by user {UserId}", topic.Id, userGuid);

                // ===== SEND EMAIL NOTIFICATIONS TO ALL SUBSCRIBERS =====
                try
                {
                    // Get all subscribers (excluding the topic creator)
                    var subscribers = await _context.Subscriptions
                        .Where(s => s.TopicId == id && s.StudentId != userGuid)
                        .Select(s => s.StudentId)
                        .ToListAsync();

                    if (subscribers.Any())
                    {
                        var topicUrl = Url.Action("Details", "Topics", new { id = id }, Request.Scheme);

                        foreach (var subscriberId in subscribers)
                        {
                            var subscriber = await _context.Students.FindAsync(subscriberId);
                            if (subscriber != null && !string.IsNullOrEmpty(subscriber.Email))
                            {
                                // Send email notification asynchronously
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _emailService.SendTopicUpdateNotificationAsync(
                                            subscriber.Email,
                                            subscriber.Name,
                                            topic.Title,
                                            topicUrl
                                        );
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to send update notification to {Email}", subscriber.Email);
                                    }
                                });
                            }
                        }

                        _logger.LogInformation("Topic update notifications queued for {Count} subscribers of topic {TopicId}",
                            subscribers.Count, id);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Error sending topic update notifications for topic {TopicId}", id);
                }

                TempData["SuccessMessage"] = "Topic updated successfully! Notifications sent to subscribers.";
                return RedirectToAction("Details", new { id = topic.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating topic {TopicId}", id);
                TempData["ErrorMessage"] = "Unable to update topic. Please try again.";
                return RedirectToAction("Details", new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMaterial(UploadMaterialViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to upload materials.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide all required information.";
                return RedirectToAction("Details", new { id = model.TopicId });
            }

            if (model.File == null || model.File.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Details", new { id = model.TopicId });
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                var topic = await _context.Topics.FindAsync(model.TopicId);

                if (topic == null)
                {
                    TempData["ErrorMessage"] = "Topic not found.";
                    return RedirectToAction("Browse");
                }

                // Verify user is the creator
                bool isCreator = (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true && topic.StudentCreatorId == userId) ||
                                (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true && topic.TutorCreatorId == userId);

                if (!isCreator)
                {
                    TempData["ErrorMessage"] = "Only the topic creator can upload materials.";
                    return RedirectToAction("Details", new { id = model.TopicId });
                }

                // Validate file size (max 50MB)
                const long maxFileSize = 50 * 1024 * 1024;
                if (model.File.Length > maxFileSize)
                {
                    TempData["ErrorMessage"] = "File size cannot exceed 50MB.";
                    return RedirectToAction("Details", new { id = model.TopicId });
                }

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx",
            ".xls", ".xlsx", ".txt", ".zip", ".rar", ".7z",
            ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".avi", ".mov",
            ".mp3", ".wav", ".cs", ".java", ".py", ".js", ".html", ".css" };

                var fileExtension = Path.GetExtension(model.File.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "File type not supported. Please upload a valid file.";
                    return RedirectToAction("Details", new { id = model.TopicId });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "materials");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.File.FileName)}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                var fileExtensions = Path.GetExtension(model.File.FileName).ToLower();
                var fileKind = GetFileKindFromExtension(fileExtensions);

                var material = new LearningMaterial
                {
                    Id = Guid.NewGuid(),
                    Title = model.Title,
                    FilePath = $"/uploads/materials/{uniqueFileName}",
                    FileType = fileKind.ToString().ToLower(),
                    TopicId = model.TopicId,
                    UploadedAt = DateTime.UtcNow
                };

                if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                {
                    material.StudentPosterId = userId;
                }
                else if (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    material.TutorPosterId = userId;
                }
                else if (userType?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    material.AdminPosterId = userId;
                }
                else
                {
                    material.StudentPosterId = userId;
                }

                _context.LearningMaterials.Add(material);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Material uploaded: {MaterialId} to topic {TopicId} by user {UserId}",
                    material.Id, model.TopicId, userId);

                TempData["SuccessMessage"] = "Material uploaded successfully!";
                return RedirectToAction("Details", new { id = model.TopicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading material to topic {TopicId}", model.TopicId);
                TempData["ErrorMessage"] = "Unable to upload material. Please try again.";
                return RedirectToAction("Details", new { id = model.TopicId });
            }
        }

        // GET: Topics/DownloadMaterial/5
        public async Task<IActionResult> DownloadMaterial(Guid id)
        {
            try
            {
                var material = await _context.LearningMaterials.FindAsync(id);

                if (material == null)
                {
                    _logger.LogWarning("Material not found: {MaterialId}", id);
                    TempData["ErrorMessage"] = "Material not found.";
                    return RedirectToAction("Browse");
                }

                // Get file path
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", material.FilePath.TrimStart('/'));

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File not found on disk: {FilePath}", filePath);
                    TempData["ErrorMessage"] = "File not found.";
                    return RedirectToAction("Details", new { id = material.TopicId });
                }

                // Get original filename from the stored path
                var fileName = Path.GetFileName(material.FilePath);
                // Remove the GUID prefix for a cleaner download name
                var displayName = fileName.Contains('_') ? fileName.Substring(fileName.IndexOf('_') + 1) : fileName;

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(material.FileType.ToString());
                return File(memory, contentType, displayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading material {MaterialId}", id);
                TempData["ErrorMessage"] = "Unable to download material. Please try again.";
                return RedirectToAction("Browse");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTopicForEdit(Guid id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Please login to edit topics." });
            }

            try
            {
                Guid userGuid = Guid.Parse(userId);
                var topic = await _context.Topics.FindAsync(id);

                if (topic == null)
                {
                    return Json(new { success = false, message = "Topic not found." });
                }

                // Verify user is the creator
                bool isCreator = (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true && topic.StudentCreatorId == userGuid) ||
                                (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true && topic.TutorCreatorId == userGuid);

                if (!isCreator)
                {
                    return Json(new { success = false, message = "Only the topic creator can edit this topic." });
                }

                // Get module name
                var module = await _context.Modules.FindAsync(topic.ModuleId);
                var moduleName = module?.ModuleName ?? "Unknown Module";

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = topic.Id,
                        title = topic.Title,
                        description = topic.Description,
                        module = moduleName,
                        priority = topic.Priority.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading topic for edit {TopicId}", id);
                return Json(new { success = false, message = "Unable to load topic data." });
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

            // Check for unread messages
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
                else if (material.AdminPosterId.HasValue)
                {
                    var admin = await _context.Administrators.FindAsync(material.AdminPosterId.Value);
                    uploaderName = admin?.Name ?? "Unknown Admin";
                }

                // Extract filename from file path
                var fileName = Path.GetFileName(material.FilePath);
                // Remove GUID prefix if it exists
                if (fileName.Contains('_'))
                {
                    fileName = fileName.Substring(fileName.IndexOf('_') + 1);
                }

                // Calculate file size from actual file on disk
                string fileSize = "Unknown size";
                try
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", material.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        var fileInfo = new FileInfo(physicalPath);
                        fileSize = FormatFileSize(fileInfo.Length);
                    }
                }
                catch
                {
                    fileSize = "Unknown size";
                }

                materialItems.Add(new TopicMaterialItem
                {
                    Id = material.Id,
                    Title = material.Title,
                    FileName = fileName,
                    FileType = material.FileType.ToString().ToLower(),
                    FileSize = fileSize,
                    UploadedBy = uploaderName,
                    UploadedAt = material.UploadedAt,
                    TimeAgo = GetTimeAgo(material.UploadedAt),
                    DownloadCount = 0
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
                    IsRead = true
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

        private string GetContentType(string fileType)
        {
            return fileType?.ToLower() switch
            {
                "pdf" => "application/pdf",
                "document" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "presentation" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "spreadsheet" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "image" => "image/jpeg",
                "video" => "video/mp4",
                "audio" => "audio/mpeg",
                "text" => "text/plain",
                "code" => "text/plain",
                "archive" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        private FileKind GetFileKindFromExtension(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => FileKind.pdf,
                ".mp4" or ".avi" or ".mov" or ".wmv" or ".mkv" => FileKind.video,
                ".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac" => FileKind.audio,
                ".ppt" or ".pptx" or ".pps" or ".ppsx" => FileKind.slide,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" => FileKind.image,
                _ => FileKind.other
            };
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