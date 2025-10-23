using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class ForumController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<ForumController> _logger;
        private readonly IEmailService _emailService;

        public ForumController(CampusLearnDbContext context, ILogger<ForumController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Forum
        public async Task<IActionResult> Index(string sortBy = "recent")
        {
            try
            {
                var posts = await _context.ForumPosts.ToListAsync();

                var viewModel = new ForumIndexViewModel
                {
                    Posts = new List<ForumPostViewModel>(),
                    CurrentSort = sortBy,
                    TotalPosts = posts.Count
                };

                foreach (var post in posts)
                {
                    // Get reply count from forum_post_reply table
                    var replyCount = await _context.ForumPostReplies
                        .CountAsync(r => r.PostId == post.Id);

                    // Extract title and content from PostContent
                    var (title, content) = ExtractTitleAndContent(post.PostContent);

                    viewModel.Posts.Add(new ForumPostViewModel
                    {
                        Id = post.Id,
                        Title = title,
                        Content = content,
                        AuthorName = post.IsAnonymous
                            ? GetCodename(post.AuthorId)
                            : GetAuthorName(post.AuthorId, post.AuthorType),
                        IsAnonymous = post.IsAnonymous,
                        CreatedAt = post.CreatedAt,
                        UpvoteCount = post.UpvoteCount,
                        DownvoteCount = post.DownvoteCount,
                        ReplyCount = replyCount,
                        NetVotes = post.GetNetVotes()
                    });
                }

                // Apply sorting
                viewModel.Posts = sortBy.ToLower() switch
                {
                    "upvoted" => viewModel.Posts.OrderByDescending(p => p.NetVotes)
                                               .ThenByDescending(p => p.CreatedAt).ToList(),
                    "oldest" => viewModel.Posts.OrderBy(p => p.CreatedAt).ToList(),
                    _ => viewModel.Posts.OrderByDescending(p => p.CreatedAt).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading forum posts");
                TempData["ErrorMessage"] = "Unable to load forum posts. Please try again.";
                return View(new ForumIndexViewModel());
            }
        }

        // GET: Forum/Create
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Please login to create a forum post.";
                return RedirectToAction("Login", "Account");
            }

            return View(new CreateForumPostViewModel());
        }

        // POST: Forum/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateForumPostViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to create a forum post.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                var post = new ForumPosts
                {
                    Id = Guid.NewGuid(),
                    PostContent = $"{model.Title}\n\n{model.Content}",
                    IsAnonymous = model.IsAnonymous,
                    CreatedAt = DateTime.UtcNow,
                    UpvoteCount = 0,
                    DownvoteCount = 0
                };

                // Set author based on user type
                if (userType?.Equals("Student", StringComparison.OrdinalIgnoreCase) == true)
                {
                    post.StudentAuthorId = userId;
                }
                else if (userType?.Equals("Tutor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    post.TutorAuthorId = userId;
                }
                else
                {
                    post.StudentAuthorId = userId; // Default to student
                }

                _context.ForumPosts.Add(post);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Forum post created: {PostId} by {UserId}", post.Id, userId);
                TempData["SuccessMessage"] = "Your post has been published successfully!";

                return RedirectToAction("Details", new { id = post.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum post");
                ModelState.AddModelError("", "Unable to create post. Please try again.");
                return View(model);
            }
        }

        // GET: Forum/Details
        public async Task<IActionResult> Details(Guid id, string sortBy = "recent")
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                Guid? currentUserId = string.IsNullOrEmpty(userIdString) ? null : Guid.Parse(userIdString);

                var post = await _context.ForumPosts.FindAsync(id);
                if (post == null)
                {
                    TempData["ErrorMessage"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                // Get all replies for this post
                var replies = await _context.ForumPostReplies
                    .Where(r => r.PostId == id)
                    .ToListAsync();

                // Get user's votes if logged in
                Dictionary<Guid, string> userVotes = new();
                if (currentUserId.HasValue)
                {
                    var votes = await _context.ForumVotes
                        .Where(v => v.UserId == currentUserId.Value && v.TargetType == "Reply")
                        .ToDictionaryAsync(v => v.TargetId, v => v.VoteType);
                    userVotes = votes;
                }

                var (title, content) = ExtractTitleAndContent(post.PostContent);

                var viewModel = new ForumPostDetailsViewModel
                {
                    Id = post.Id,
                    Title = title,
                    Content = content,
                    AuthorName = post.IsAnonymous
                        ? GetCodename(post.AuthorId)
                        : GetAuthorName(post.AuthorId, post.AuthorType),
                    IsAnonymous = post.IsAnonymous,
                    CreatedAt = post.CreatedAt,
                    UpvoteCount = post.UpvoteCount,
                    DownvoteCount = post.DownvoteCount,
                    NetVotes = post.GetNetVotes(),
                    Replies = new List<ForumReplyViewModel>(),
                    CurrentSort = sortBy
                };

                // Build ALL reply view models first
                var allReplyViewModels = new List<ForumReplyViewModel>();

                foreach (var reply in replies)
                {
                    var replyAuthorId = reply.StudentPosterId ?? reply.TutorPosterId ?? Guid.Empty;
                    var replyAuthorType = reply.StudentPosterId.HasValue ? "Student" : "Tutor";

                    var replyViewModel = new ForumReplyViewModel
                    {
                        Id = reply.ReplyId,
                        ParentReplyId = reply.ParentReplyId,
                        Content = reply.ReplyContent,
                        AuthorName = reply.IsAnonymous
                            ? GetCodename(replyAuthorId)
                            : GetAuthorName(replyAuthorId, replyAuthorType),
                        IsAnonymous = reply.IsAnonymous,
                        CreatedAt = reply.CreatedAt,
                        UpvoteCount = reply.UpvoteCount,
                        DownvoteCount = reply.DownvoteCount,
                        NetVotes = reply.GetNetVotes(),
                        HasUserUpvoted = userVotes.ContainsKey(reply.ReplyId) && userVotes[reply.ReplyId] == "Upvote",
                        HasUserDownvoted = userVotes.ContainsKey(reply.ReplyId) && userVotes[reply.ReplyId] == "Downvote",
                        NestedReplies = new List<ForumReplyViewModel>()
                    };

                    allReplyViewModels.Add(replyViewModel);
                }

                // Separate top-level replies from nested ones
                var topLevelReplies = allReplyViewModels.Where(r => r.ParentReplyId == null).ToList();
                var nestedReplies = allReplyViewModels.Where(r => r.ParentReplyId != null).ToList();

                // Attach nested replies to their parents
                foreach (var nestedReply in nestedReplies)
                {
                    var parent = FindReplyById(topLevelReplies, nestedReply.ParentReplyId.Value);
                    if (parent != null)
                    {
                        parent.NestedReplies.Add(nestedReply);
                    }
                }

                // Apply sorting ONLY to top-level replies
                topLevelReplies = sortBy.ToLower() switch
                {
                    "upvoted" => topLevelReplies.OrderByDescending(r => r.NetVotes)
                                               .ThenByDescending(r => r.CreatedAt).ToList(),
                    "oldest" => topLevelReplies.OrderBy(r => r.CreatedAt).ToList(),
                    _ => topLevelReplies.OrderByDescending(r => r.CreatedAt).ToList()
                };

                // IMPORTANT: Only set top-level replies
                viewModel.Replies = topLevelReplies;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post details for {PostId}", id);
                TempData["ErrorMessage"] = "Unable to load post. Please try again.";
                return RedirectToAction("Index");
            }
        }

        private ForumReplyViewModel? FindReplyById(List<ForumReplyViewModel> replies, Guid replyId)
        {
            foreach (var reply in replies)
            {
                if (reply.Id == replyId)
                    return reply;

                var found = FindReplyById(reply.NestedReplies, replyId);
                if (found != null)
                    return found;
            }
            return null;
        }

        // Updated Reply method to support nested replies and EMAIL NOTIFICATIONS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(CreateReplyViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to reply.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid reply content.";
                return RedirectToAction("Details", new { id = model.ParentPostId });
            }

            try
            {
                var userId = Guid.Parse(userIdString);

                var reply = new ForumPostReply
                {
                    ReplyId = Guid.NewGuid(),
                    PostId = model.ParentPostId,
                    ParentReplyId = model.ParentReplyId,
                    ReplyContent = model.Content,
                    IsAnonymous = model.IsAnonymous,
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

                _context.ForumPostReplies.Add(reply);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reply created: {ReplyId} to post {PostId}", reply.ReplyId, model.ParentPostId);

                // ===== SEND EMAIL NOTIFICATION =====
                try
                {
                    // Get the original post
                    var post = await _context.ForumPosts.FindAsync(model.ParentPostId);
                    if (post != null)
                    {
                        // Get the post author's details
                        string authorEmail = null;
                        string authorName = null;

                        if (post.StudentAuthorId.HasValue)
                        {
                            var author = await _context.Students.FindAsync(post.StudentAuthorId.Value);
                            if (author != null && author.Id != userId) // Don't send email to yourself
                            {
                                authorEmail = author.Email;
                                authorName = author.Name;
                            }
                        }
                        else if (post.TutorAuthorId.HasValue)
                        {
                            var author = await _context.Tutors.FindAsync(post.TutorAuthorId.Value);
                            if (author != null && author.Id != userId)
                            {
                                authorEmail = author.Email;
                                authorName = author.Name;
                            }
                        }

                        // Get replier's name
                        string replierName = "Anonymous User";
                        if (!model.IsAnonymous)
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

                        // Send email if we have the author's email
                        if (!string.IsNullOrEmpty(authorEmail))
                        {
                            var (postTitle, _) = ExtractTitleAndContent(post.PostContent);
                            var postUrl = Url.Action("Details", "Forum", new { id = post.Id }, Request.Scheme);

                            // Truncate reply content for email (first 200 characters)
                            var replyPreview = model.Content.Length > 200
                                ? model.Content.Substring(0, 200) + "..."
                                : model.Content;

                            await _emailService.SendForumReplyNotificationAsync(
                                authorEmail,
                                authorName,
                                postTitle,
                                replierName,
                                replyPreview,
                                postUrl
                            );

                            _logger.LogInformation("Email notification sent to {Email} for reply on post {PostId}",
                                authorEmail, post.Id);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the reply operation
                    _logger.LogError(emailEx, "Failed to send email notification for forum reply {ReplyId}", reply.ReplyId);
                }

                TempData["SuccessMessage"] = "Your reply has been posted!";
                return RedirectToAction("Details", new { id = model.ParentPostId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply");
                TempData["ErrorMessage"] = "Unable to post reply. Please try again.";
                return RedirectToAction("Details", new { id = model.ParentPostId });
            }
        }

        // POST: Forum/Upvote
        [HttpPost]
        public async Task<IActionResult> Upvote([FromBody] VoteRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var userId = Guid.Parse(userIdString);
                var post = await _context.ForumPosts.FindAsync(request.Id);

                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found" });
                }

                // Check for existing vote
                var existingVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Post");

                if (existingVote != null)
                {
                    if (existingVote.VoteType == "Upvote")
                    {
                        // Remove upvote
                        if (post.UpvoteCount > 0) post.UpvoteCount--;
                        _context.ForumVotes.Remove(existingVote);
                    }
                    else
                    {
                        // Change downvote to upvote
                        if (post.DownvoteCount > 0) post.DownvoteCount--;
                        post.Upvote();
                        existingVote.VoteType = "Upvote";
                        existingVote.CreatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // New upvote
                    post.Upvote();
                    _context.ForumVotes.Add(new ForumVote
                    {
                        UserId = userId,
                        UserType = userType ?? "Student",
                        TargetId = request.Id,
                        TargetType = "Post",
                        VoteType = "Upvote"
                    });
                }

                await _context.SaveChangesAsync();

                var currentVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Post");

                return Json(new
                {
                    success = true,
                    netVotes = post.GetNetVotes(),
                    hasUserUpvoted = currentVote?.VoteType == "Upvote",
                    hasUserDownvoted = currentVote?.VoteType == "Downvote"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upvoting post {PostId}", request.Id);
                return Json(new { success = false, message = "Unable to upvote" });
            }
        }

        // POST: Forum/Downvote
        [HttpPost]
        public async Task<IActionResult> Downvote([FromBody] VoteRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var userId = Guid.Parse(userIdString);
                var post = await _context.ForumPosts.FindAsync(request.Id);

                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found" });
                }

                // Check for existing vote
                var existingVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Post");

                if (existingVote != null)
                {
                    if (existingVote.VoteType == "Downvote")
                    {
                        // Remove downvote
                        if (post.DownvoteCount > 0) post.DownvoteCount--;
                        _context.ForumVotes.Remove(existingVote);
                    }
                    else
                    {
                        // Change upvote to downvote
                        if (post.UpvoteCount > 0) post.UpvoteCount--;
                        post.Downvote();
                        existingVote.VoteType = "Downvote";
                        existingVote.CreatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // New downvote
                    post.Downvote();
                    _context.ForumVotes.Add(new ForumVote
                    {
                        UserId = userId,
                        UserType = userType ?? "Student",
                        TargetId = request.Id,
                        TargetType = "Post",
                        VoteType = "Downvote"
                    });
                }

                await _context.SaveChangesAsync();

                var currentVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Post");

                return Json(new
                {
                    success = true,
                    netVotes = post.GetNetVotes(),
                    hasUserUpvoted = currentVote?.VoteType == "Upvote",
                    hasUserDownvoted = currentVote?.VoteType == "Downvote"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downvoting post {PostId}", request.Id);
                return Json(new { success = false, message = "Unable to downvote" });
            }
        }

        // Add new methods for reply voting
        [HttpPost]
        public async Task<IActionResult> UpvoteReply([FromBody] VoteRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var userId = Guid.Parse(userIdString);
                var reply = await _context.ForumPostReplies.FindAsync(request.Id);

                if (reply == null)
                {
                    return Json(new { success = false, message = "Reply not found" });
                }

                // Check for existing vote
                var existingVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Reply");

                if (existingVote != null)
                {
                    if (existingVote.VoteType == "Upvote")
                    {
                        // Remove upvote
                        reply.RemoveUpvote();
                        _context.ForumVotes.Remove(existingVote);
                    }
                    else
                    {
                        // Change downvote to upvote
                        reply.RemoveDownvote();
                        reply.Upvote();
                        existingVote.VoteType = "Upvote";
                        existingVote.CreatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // New upvote
                    reply.Upvote();
                    _context.ForumVotes.Add(new ForumVote
                    {
                        UserId = userId,
                        UserType = userType ?? "Student",
                        TargetId = request.Id,
                        TargetType = "Reply",
                        VoteType = "Upvote"
                    });
                }

                await _context.SaveChangesAsync();

                // Check current user's vote status
                var currentVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Reply");

                return Json(new
                {
                    success = true,
                    netVotes = reply.GetNetVotes(),
                    hasUserUpvoted = currentVote?.VoteType == "Upvote",
                    hasUserDownvoted = currentVote?.VoteType == "Downvote"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upvoting reply {ReplyId}", request.Id);
                return Json(new { success = false, message = "Unable to upvote" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownvoteReply([FromBody] VoteRequest request)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userType = HttpContext.Session.GetString("UserType");

            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var userId = Guid.Parse(userIdString);
                var reply = await _context.ForumPostReplies.FindAsync(request.Id);

                if (reply == null)
                {
                    return Json(new { success = false, message = "Reply not found" });
                }

                // Check for existing vote
                var existingVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Reply");

                if (existingVote != null)
                {
                    if (existingVote.VoteType == "Downvote")
                    {
                        // Remove downvote
                        reply.RemoveDownvote();
                        _context.ForumVotes.Remove(existingVote);
                    }
                    else
                    {
                        // Change upvote to downvote
                        reply.RemoveUpvote();
                        reply.Downvote();
                        existingVote.VoteType = "Downvote";
                        existingVote.CreatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // New downvote
                    reply.Downvote();
                    _context.ForumVotes.Add(new ForumVote
                    {
                        UserId = userId,
                        UserType = userType ?? "Student",
                        TargetId = request.Id,
                        TargetType = "Reply",
                        VoteType = "Downvote"
                    });
                }

                await _context.SaveChangesAsync();

                // Check current user's vote status
                var currentVote = await _context.ForumVotes
                    .FirstOrDefaultAsync(v => v.UserId == userId &&
                                             v.TargetId == request.Id &&
                                             v.TargetType == "Reply");

                return Json(new
                {
                    success = true,
                    netVotes = reply.GetNetVotes(),
                    hasUserUpvoted = currentVote?.VoteType == "Upvote",
                    hasUserDownvoted = currentVote?.VoteType == "Downvote"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downvoting reply {ReplyId}", request.Id);
                return Json(new { success = false, message = "Unable to downvote" });
            }
        }

        // Helper methods
        private string GetAuthorName(Guid authorId, string authorType)
        {
            try
            {
                if (authorType == "Student")
                {
                    var student = _context.Students.Find(authorId);
                    return student?.Name ?? "Unknown Student";
                }
                else if (authorType == "Tutor")
                {
                    var tutor = _context.Tutors.Find(authorId);
                    return tutor?.Name ?? "Unknown Tutor";
                }
                return "Unknown User";
            }
            catch
            {
                return "Unknown User";
            }
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

        private (string title, string content) ExtractTitleAndContent(string postContent)
        {
            if (string.IsNullOrWhiteSpace(postContent))
            {
                return ("Untitled Post", "");
            }

            var parts = postContent.Split(new[] { "\n\n" }, 2, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }

            // If no double newline, use first line as title
            var lines = postContent.Split('\n');
            return (lines[0].Trim(), postContent);
        }
    }

    // Request model for voting
    public class VoteRequest
    {
        public Guid Id { get; set; }
    }

    // Codename generator for anonymous users
    public static class Codename
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