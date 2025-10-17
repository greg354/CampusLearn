using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.ViewModels;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            return Guid.Parse(userIdClaim);
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

            foreach (var message in messages.Where(m =>
                (m.StudentReceiverId == currentUserId || m.TutorReceiverId == currentUserId) && !m.IsRead))
            {
                await _messageService.MarkAsReadAsync(message.Id, currentUserId);
            }

            var viewModel = new ConversationViewModel
            {
                OtherUserId = otherUserId,
                OtherUserName = otherUserName,
                OtherUserRole = otherUserRole,
                Messages = messages,
                NewMessage = new NewMessageViewModel { ReceiverId = otherUserId }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(NewMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = GetCurrentUserId();
                var message = await _messageService.SendMessageAsync(currentUserId, model.ReceiverId, model.Content);
                return RedirectToAction("Conversation", new { otherUserId = model.ReceiverId });
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
                    var name = u.GetType().GetProperty("Name")?.GetValue(u)?.ToString();
                    var email = u.GetType().GetProperty("Email")?.GetValue(u)?.ToString();
                    var type = u.GetType().GetProperty("Type")?.GetValue(u)?.ToString();

                    if (id is Guid userId && userId != currentUserId)
                    {
                        return new
                        {
                            id = userId,
                            text = $"{name} ({email}) - {type}"
                        };
                    }
                    return null;
                })
                .Where(x => x != null)
                .ToList();

            return Json(result);
        }
    }
}