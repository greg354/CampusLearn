using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampusLearnPlatform.Controllers
{
    [Authorize]
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new InvalidOperationException("User is not authenticated.");
            }

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            throw new InvalidOperationException("Invalid user ID format.");
        }

        public async Task<IActionResult> Index()
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

        // New Compose Action
        public IActionResult Compose()
        {
            var model = new ComposeMessageViewModel();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(Guid otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);

            string otherUserName = "Unknown User";
            string otherUserRole = "User";

            var student = await _context.Students.FindAsync(otherUserId);
            var tutor = await _context.Tutors.FindAsync(otherUserId);

            if (student != null)
            {
                otherUserName = student.Name;
                otherUserRole = "Student";
            }
            else if (tutor != null)
            {
                otherUserName = tutor.Name;
                otherUserRole = "Tutor";
            }

            // Mark messages as read
            foreach (var message in messages.Where(m =>
                (m.StudentReceiverId == currentUserId || m.TutorReceiverId == currentUserId) && !m.IsRead))
            {
                await _messageService.MarkAsReadAsync(message.Id, currentUserId);
            }

            // Convert to MessageViewModel - Handle nullable GUIDs safely
            var messageViewModels = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                // Safely handle nullable GUIDs with proper casting
                ReceiverId = m.StudentReceiverId != currentUserId ?
                    (m.StudentReceiverId ?? Guid.Empty) :
                    (m.TutorReceiverId ?? Guid.Empty),
                Timestamp = GetMessageTimestamp(m),
                IsRead = m.IsRead,
                IsSentByCurrentUser = m.SenderId == currentUserId,
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

            return View(viewModel);
        }

        // Helper method to safely get timestamp from PrivateMessage
        private DateTime GetMessageTimestamp(PrivateMessage message)
        {
            if (message == null) return DateTime.Now;

            // Try to get timestamp using reflection for common property names
            var type = message.GetType();
            var timestampProperties = new[] { "CreatedAt", "Timestamp", "SentAt", "CreatedDate", "DateSent" };

            foreach (var propName in timestampProperties)
            {
                var property = type.GetProperty(propName);
                if (property != null && property.PropertyType == typeof(DateTime))
                {
                    var value = property.GetValue(message);
                    if (value is DateTime dateTimeValue)
                    {
                        return dateTimeValue;
                    }
                }
            }

            // Fallback to current time
            return DateTime.Now;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(NewMessageViewModel model)
        {
            if (ModelState.IsValid)
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
            var currentUserId = GetCurrentUserId();
            var success = await _messageService.MarkAsReadAsync(messageId, currentUserId);
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return Json(new List<object>());

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

        [HttpGet]
        public async Task<JsonResult> GetConversation(Guid otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);

            var messageViewModels = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                // Safely handle nullable GUIDs
                ReceiverId = m.StudentReceiverId != currentUserId ?
                    (m.StudentReceiverId ?? Guid.Empty) :
                    (m.TutorReceiverId ?? Guid.Empty),
                Timestamp = GetMessageTimestamp(m),
                IsRead = m.IsRead,
                IsSentByCurrentUser = m.SenderId == currentUserId
            }).ToList();

            return Json(new { messages = messageViewModels });
        }
    }
}