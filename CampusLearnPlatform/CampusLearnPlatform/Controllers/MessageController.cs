using CampusLearnPlatform.Data;
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
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                throw new InvalidOperationException("User is not authenticated. Please log in.");

            if (Guid.TryParse(userIdString, out var id)) return id;
            throw new InvalidOperationException("Invalid user ID format in session.");
        }

        public async Task<IActionResult> Index()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
            {
                TempData["ErrorMessage"] = "Please login to view messages.";
                return RedirectToAction("Index", "Login");
            }

            var currentUserId = GetCurrentUserId();
            var conversations = await _messageService.GetRecentConversationsAsync(currentUserId);
            var unread = await _messageService.GetUnreadCountAsync(currentUserId);

            var vm = new MessageIndexViewModel
            {
                RecentConversations = conversations,
                UnreadCount = unread
            };
            return View(vm);
        }

        public IActionResult Compose()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                TempData["ErrorMessage"] = "Please login to send messages.";
                return RedirectToAction("Index", "Login");
            }
            return View(new ComposeMessageViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Conversation(Guid otherUserId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                TempData["ErrorMessage"] = "Please login to view conversations.";
                return RedirectToAction("Index", "Login");
            }

            var currentUserId = GetCurrentUserId();
            var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);

            string otherUserName = "Unknown User";
            string otherUserRole = "User";
            var student = await _context.Students.FindAsync(otherUserId);
            var tutor = await _context.Tutors.FindAsync(otherUserId);
            if (student != null) { otherUserName = student.Name; otherUserRole = "Student"; }
            else if (tutor != null) { otherUserName = tutor.Name; otherUserRole = "Tutor"; }

            var messageVMs = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                Content = m.MessageContent,
                SenderId = m.StudentSenderId ?? m.TutorSenderId ?? Guid.Empty,
                ReceiverId = m.StudentReceiverId ?? m.TutorReceiverId ?? Guid.Empty,
                Timestamp = m.Timestamp,
                IsRead = false, // no is_read column right now
                IsSentByCurrentUser = (m.StudentSenderId == currentUserId || m.TutorSenderId == currentUserId),
                Status = "Delivered"
            }).ToList();

            var vm = new ConversationViewModel
            {
                OtherUserId = otherUserId,
                OtherUserName = otherUserName,
                OtherUserRole = otherUserRole,
                Messages = messageVMs,
                NewMessage = new NewMessageViewModel { ReceiverId = otherUserId }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(NewMessageViewModel model)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, error = "Please login to send messages." });

                TempData["ErrorMessage"] = "Please login to send messages.";
                return RedirectToAction("Index", "Login");
            }

            if (!ModelState.IsValid)
            {
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

            var currentUserId = GetCurrentUserId();
            var msg = await _messageService.SendMessageAsync(currentUserId, model.ReceiverId, model.Content);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, messageId = msg.Id });

            return RedirectToAction("Conversation", new { otherUserId = model.ReceiverId });
        }

        // ---------- SEARCH ENDPOINT ----------
        // Accepts ?term=, ?q=, or ?query= and returns { results: [ { id, text, role } ] }
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SearchUsers(string term, string q, string query)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return Json(Array.Empty<object>());

            var raw = term ?? q ?? query ?? "";
            var results = await _messageService.SearchUsersAsync(raw);

            var currentUserId = Guid.Parse(HttpContext.Session.GetString("UserId")!);

            var items = results
                .Select(u =>
                {
                    var id = (Guid)u.GetType().GetProperty("Id")!.GetValue(u)!;
                    if (id == currentUserId) return null; // don't return self

                    var name = u.GetType().GetProperty("Name")!.GetValue(u)!.ToString();
                    var email = u.GetType().GetProperty("Email")!.GetValue(u)!.ToString();
                    var type = u.GetType().GetProperty("Type")!.GetValue(u)!.ToString();

                    return new
                    {
                        id,
                        text = $"{name} ({email})",
                        role = type
                    };
                })
                .Where(x => x != null)
                .Take(20)
                .ToList();

            // IMPORTANT: return a plain array to match Compose.cshtml
            return Json(items);
        }

        public async Task<JsonResult> GetConversation(Guid otherUserId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                return Json(new { messages = new List<MessageViewModel>() });

            var currentUserId = GetCurrentUserId();
            var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);

            var items = messages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                Content = m.MessageContent,
                SenderId = m.StudentSenderId ?? m.TutorSenderId ?? Guid.Empty,
                ReceiverId = m.StudentReceiverId ?? m.TutorReceiverId ?? Guid.Empty,
                Timestamp = m.Timestamp,
                IsRead = false,
                IsSentByCurrentUser = (m.StudentSenderId == currentUserId || m.TutorSenderId == currentUserId),
                Status = "Delivered"
            }).ToList();

            return Json(new { messages = items });
        }
    }
}
