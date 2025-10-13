using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Models.Communication;
using System.Security.Cryptography;
using System.Text;

namespace CampusLearnPlatform.Controllers
{
    public class ForumController : Controller
    {
        private readonly CampusLearnDbContext _context;
        private readonly ILogger<ForumController> _logger;

        public ForumController(CampusLearnDbContext context, ILogger<ForumController> logger)
        {
            _context = context;
            _logger = logger;
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

        // GET: Forum/Details/5
        public async Task<IActionResult> Details(Guid id, string sortBy = "recent")
        {
            try
            {
                var post = await _context.ForumPosts.FindAsync(id);
                if (post == null)
                {
                    TempData["ErrorMessage"] = "Post not found.";
                    return RedirectToAction("Index");
                }

                // Get replies from forum_post_reply table
                var replies = await _context.ForumPostReplies
                    .Where(r => r.PostId == id)
                    .ToListAsync();

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

                foreach (var reply in replies)
                {
                    var replyAuthorId = reply.StudentPosterId ?? reply.TutorPosterId ?? Guid.Empty;
                    var replyAuthorType = reply.StudentPosterId.HasValue ? "Student" : "Tutor";

                    viewModel.Replies.Add(new ForumReplyViewModel
                    {
                        Id = reply.ReplyId,
                        Content = reply.ReplyContent,
                        AuthorName = reply.IsAnonymous
                            ? GetCodename(replyAuthorId)
                            : GetAuthorName(replyAuthorId, replyAuthorType),
                        IsAnonymous = reply.IsAnonymous,
                        CreatedAt = reply.CreatedAt,
                        UpvoteCount = 0, // Replies don't have votes in current schema
                        DownvoteCount = 0,
                        NetVotes = 0
                    });
                }

                // Apply sorting to replies
                viewModel.Replies = sortBy.ToLower() switch
                {
                    "oldest" => viewModel.Replies.OrderBy(r => r.CreatedAt).ToList(),
                    _ => viewModel.Replies.OrderByDescending(r => r.CreatedAt).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post details for {PostId}", id);
                TempData["ErrorMessage"] = "Unable to load post. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // POST: Forum/Reply
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
                    reply.StudentPosterId = userId; // Default to student
                }

                _context.ForumPostReplies.Add(reply);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reply created: {ReplyId} to post {PostId}", reply.ReplyId, model.ParentPostId);
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
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var post = await _context.ForumPosts.FindAsync(request.Id);
                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found" });
                }

                post.Upvote();
                await _context.SaveChangesAsync();

                return Json(new { success = true, netVotes = post.GetNetVotes() });
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
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, message = "Please login to vote" });
            }

            try
            {
                var post = await _context.ForumPosts.FindAsync(request.Id);
                if (post == null)
                {
                    return Json(new { success = false, message = "Post not found" });
                }

                post.Downvote();
                await _context.SaveChangesAsync();

                return Json(new { success = true, netVotes = post.GetNetVotes() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downvoting post {PostId}", request.Id);
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