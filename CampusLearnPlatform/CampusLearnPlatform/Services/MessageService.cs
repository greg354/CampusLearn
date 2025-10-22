using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Services
{
    public class MessageService : IMessageService
    {
        private readonly CampusLearnDbContext _context;

        public MessageService(CampusLearnDbContext context)
        {
            _context = context;
        }

        public async Task<List<PrivateMessage>> GetConversationAsync(Guid currentUserId, Guid otherUserId)
        {
            try
            {
                var messages = await _context.Messages
                    .Where(m =>
                        // current -> other
                        (m.StudentSenderId == currentUserId && m.TutorReceiverId == otherUserId) ||
                        (m.TutorSenderId == currentUserId && m.StudentReceiverId == otherUserId) ||
                        // other -> current
                        (m.StudentReceiverId == currentUserId && m.TutorSenderId == otherUserId) ||
                        (m.TutorReceiverId == currentUserId && m.StudentSenderId == otherUserId)
                    )
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                return messages;
            }
            catch
            {
                return new List<PrivateMessage>();
            }
        }

        public async Task<PrivateMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.");

            // detect roles by presence in tables
            var senderIsStudent = await _context.Students.AnyAsync(s => s.Id == senderId);
            var receiverIsTutor = await _context.Tutors.AnyAsync(t => t.Id == receiverId);
            var senderIsTutor = await _context.Tutors.AnyAsync(t => t.Id == senderId);
            var receiverIsStudent = await _context.Students.AnyAsync(s => s.Id == receiverId);

            var msg = new PrivateMessage
            {
                MessageContent = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            if (senderIsStudent && receiverIsTutor)
            {
                msg.StudentSenderId = senderId;
                msg.TutorReceiverId = receiverId;
            }
            else if (senderIsTutor && receiverIsStudent)
            {
                msg.TutorSenderId = senderId;
                msg.StudentReceiverId = receiverId;
            }
            else
            {
                // fallback — store something to avoid losing messages
                msg.StudentSenderId = senderId;
                msg.TutorReceiverId = receiverId;
            }

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();
            return msg;
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId, Guid userId)
        {
            // flags are NotMapped without columns; keep method no-op/true for compatibility
            var exists = await _context.Messages
                .AnyAsync(m => m.Id == messageId &&
                               (m.StudentReceiverId == userId || m.TutorReceiverId == userId));
            return exists;
        }

        public async Task<List<ConversationPreview>> GetRecentConversationsAsync(Guid userId)
        {
            var userMessages = await _context.Messages
                .Where(m => (m.StudentSenderId == userId || m.TutorSenderId == userId ||
                             m.StudentReceiverId == userId || m.TutorReceiverId == userId))
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToListAsync();

            var conversations = new Dictionary<Guid, ConversationPreview>();

            foreach (var m in userMessages)
            {
                Guid otherUserId = Guid.Empty;
                string otherUserName = "User";

                if (m.StudentSenderId == userId)
                {
                    otherUserId = m.TutorReceiverId ?? Guid.Empty;
                    var tutor = await _context.Tutors.FindAsync(otherUserId);
                    otherUserName = tutor?.Name ?? "Tutor";
                }
                else if (m.TutorSenderId == userId)
                {
                    otherUserId = m.StudentReceiverId ?? Guid.Empty;
                    var student = await _context.Students.FindAsync(otherUserId);
                    otherUserName = student?.Name ?? "Student";
                }
                else if (m.StudentReceiverId == userId)
                {
                    otherUserId = m.TutorSenderId ?? Guid.Empty;
                    var tutor = await _context.Tutors.FindAsync(otherUserId);
                    otherUserName = tutor?.Name ?? "Tutor";
                }
                else if (m.TutorReceiverId == userId)
                {
                    otherUserId = m.StudentSenderId ?? Guid.Empty;
                    var student = await _context.Students.FindAsync(otherUserId);
                    otherUserName = student?.Name ?? "Student";
                }

                if (otherUserId == Guid.Empty) continue;

                if (!conversations.ContainsKey(otherUserId))
                {
                    conversations[otherUserId] = new ConversationPreview
                    {
                        OtherUserId = otherUserId,
                        OtherUserName = otherUserName,
                        LastMessage = m.MessageContent,
                        LastMessageTime = m.Timestamp,
                        UnreadCount = 0 // without is_read column we keep this 0
                    };
                }
            }

            return conversations.Values
                .OrderByDescending(c => c.LastMessageTime)
                .Take(20)
                .ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            // no is_read column → always 0 (keeps UI from breaking)
            return 0;
        }

        public async Task<List<object>> SearchUsersAsync(string query)
        {
            var q = (query ?? string.Empty).Trim();
            if (q.Length < 2) return new List<object>();

            // CASE-INSENSITIVE search using Postgres ILIKE
            var students = await _context.Students
                .Where(s => EF.Functions.ILike(s.Name, $"%{q}%") || EF.Functions.ILike(s.Email, $"%{q}%"))
                .Select(s => new { Id = s.Id, Name = s.Name, Email = s.Email, Type = "Student" })
                .Take(10)
                .ToListAsync();

            var tutors = await _context.Tutors
                .Where(t => EF.Functions.ILike(t.Name, $"%{q}%") || EF.Functions.ILike(t.Email, $"%{q}%"))
                .Select(t => new { Id = t.Id, Name = t.Name, Email = t.Email, Type = "Tutor" })
                .Take(10)
                .ToListAsync();

            return students.Cast<object>().Concat(tutors.Cast<object>()).ToList();
        }
    }
}
