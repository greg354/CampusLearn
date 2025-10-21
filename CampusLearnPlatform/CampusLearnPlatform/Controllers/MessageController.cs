using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Controllers
{
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly CampusLearnDbContext _context;

        public MessageController(IMessageService messageService, CampusLearnDbContext context)
        {
            _messageService = messageService;
            _context = context;
        }

        private Guid GetCurrentUserId()
        {
            // Use session like ForumController instead of User.Identity
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                throw new InvalidOperationException("User is not authenticated. Please log in.");
            }

            if (Guid.TryParse(userIdString, out Guid userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Invalid user ID format in session.");
        }

        public async Task<IActionResult> Index()
        {
            // Check if user is logged in using session (like ForumController)
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to view messages.";
                return RedirectToAction("Index", "Login");
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var conversations = await _messageService.GetRecentConversationsAsync(currentUserId);
                var unreadCount = await _messageService.GetUnreadCountAsync(currentUserId);

                var viewModel = new MessageIndexViewModel
                {
                    RecentConversations = conversations,
                    UnreadCount = unreadCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading messages. Please try again.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // New Compose Action
        public IActionResult Compose()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to send messages.";
                return RedirectToAction("Index", "Login");
            }

            var model = new ComposeMessageViewModel();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(Guid otherUserId)
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to view conversations.";
                return RedirectToAction("Index", "Login");
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                Console.WriteLine($"Loading conversation between {currentUserId} and {otherUserId}");

                var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);
                Console.WriteLine($"Found {messages?.Count ?? 0} messages");

                string otherUserName = "Unknown User";
                string otherUserRole = "User";

                // Try to get the other user's details
                try
                {
                    var student = await _context.Students.FindAsync(otherUserId);
                    var tutor = await _context.Tutors.FindAsync(otherUserId);

                    if (student != null)
                    {
                        otherUserName = student.Name;
                        otherUserRole = "Student";
                        Console.WriteLine($"Other user is student: {otherUserName}");
                    }
                    else if (tutor != null)
                    {
                        otherUserName = tutor.Name;
                        otherUserRole = "Tutor";
                        Console.WriteLine($"Other user is tutor: {otherUserName}");
                    }
                    else
                    {
                        Console.WriteLine($"No user found with ID: {otherUserId}");
                    }
                }
                catch (Exception userEx)
                {
                    Console.WriteLine($"Error getting user details: {userEx.Message}");
                    // Continue with default values
                }

                // Mark messages as read
                try
                {
                    foreach (var message in messages.Where(m =>
                        (m.StudentReceiverId == currentUserId || m.TutorReceiverId == currentUserId) && !m.IsRead))
                    {
                        await _messageService.MarkAsReadAsync(message.Id, currentUserId);
                    }
                }
                catch (Exception readEx)
                {
                    Console.WriteLine($"Error marking messages as read: {readEx.Message}");
                    // Continue anyway
                }

                // Convert to MessageViewModel
                var messageViewModels = messages.Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    Content = m.MessageContent,
                    SenderId = m.StudentSenderId ?? m.TutorSenderId ?? Guid.Empty,
                    ReceiverId = m.StudentReceiverId ?? m.TutorReceiverId ?? Guid.Empty,
                    Timestamp = m.Timestamp,
                    IsRead = m.IsRead,
                    IsSentByCurrentUser = (m.StudentSenderId == currentUserId || m.TutorSenderId == currentUserId),
                    Status = m.IsRead ? "Read" : "Delivered"
                }).ToList();

                var viewModel = new ConversationViewModel
                {
                    OtherUserId = otherUserId,
                    OtherUserName = otherUserName,
                    OtherUserRole = otherUserRole,
                    Messages = messageViewModels,
                    NewMessage = new NewMessageViewModel { ReceiverId = otherUserId }
                };

                Console.WriteLine("Successfully loaded conversation view model");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in Conversation action: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Error loading conversation. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(NewMessageViewModel model)
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, error = "Please login to send messages." });
                }
                TempData["ErrorMessage"] = "Please login to send messages.";
                return RedirectToAction("Index", "Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUserId = GetCurrentUserId();
                    var message = await _messageService.SendMessageAsync(currentUserId, model.ReceiverId, model.Content);

                    // If AJAX request, return JSON
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, messageId = message.Id });
                    }

                    return RedirectToAction("Conversation", new { otherUserId = model.ReceiverId });
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, error = "Error sending message." });
                    }
                    TempData["ErrorMessage"] = "Error sending message. Please try again.";
                    return RedirectToAction("Conversation", new { otherUserId = model.ReceiverId });
                }
            }

            // If AJAX request with errors, return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = false,
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            return RedirectToAction("Conversation", new { otherUserId = model.ReceiverId });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid messageId)
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { success = false, error = "Please login." });
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var success = await _messageService.MarkAsReadAsync(messageId, currentUserId);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error marking message as read." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new List<object>());
            }

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>());

            try
            {
                var users = await _messageService.SearchUsersAsync(query);
                var currentUserId = GetCurrentUserId();

                var result = users
                    .Select(u => {
                        var id = u.GetType().GetProperty("Id")?.GetValue(u);
                        var name = u.GetType().GetProperty("Name")?.GetValue(u)?.ToString() ?? "Unknown";
                        var email = u.GetType().GetProperty("Email")?.GetValue(u)?.ToString() ?? "";
                        var type = u.GetType().GetProperty("Type")?.GetValue(u)?.ToString() ?? "User";

                        // Safely handle nullable GUID
                        if (id is Guid userId && userId != currentUserId)
                        {
                            return new
                            {
                                id = userId,
                                text = $"{name} ({email})",
                                role = type
                            };
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetConversation(Guid otherUserId)
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                return Json(new { messages = new List<MessageViewModel>() });
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);

                // FIXED: Use correct property names and direct timestamp access
                var messageViewModels = messages.Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    Content = m.MessageContent, // Fixed property name
                    SenderId = m.StudentSenderId ?? m.TutorSenderId ?? Guid.Empty,
                    ReceiverId = m.StudentReceiverId ?? m.TutorReceiverId ?? Guid.Empty,
                    Timestamp = m.Timestamp, // Fixed: use direct property
                    IsRead = m.IsRead,
                    IsSentByCurrentUser = (m.StudentSenderId == currentUserId || m.TutorSenderId == currentUserId)
                }).ToList();

                return Json(new { messages = messageViewModels });
            }
            catch (Exception ex)
            {
                return Json(new { messages = new List<MessageViewModel>() });
            }
        }
    }
}