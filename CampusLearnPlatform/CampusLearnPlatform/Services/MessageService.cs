using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Services
{
    public class MessageService: IMessageService
    {
        private readonly CampusLearnDbContext _context;

        public MessageService(CampusLearnDbContext context)
        {
            _context = context;
        }

        public async Task<List<PrivateMessage>> GetConversationAsync(Guid currentUserId, Guid otherUserId)
        {
            return await _context.Messages
                .Where(m =>
                    (m.StudentSenderId == currentUserId && m.TutorReceiverId == otherUserId) ||
                    (m.TutorSenderId == currentUserId && m.StudentReceiverId == otherUserId) ||
                    (m.StudentReceiverId == currentUserId && m.TutorSenderId == otherUserId) ||
                    (m.TutorReceiverId == currentUserId && m.StudentSenderId == otherUserId)
                )
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<PrivateMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            var isStudentSender = await _context.Students.AnyAsync(s => s.Id == senderId);
            var isTutorReceiver = await _context.Tutors.AnyAsync(t => t.Id == receiverId);

            var message = new PrivateMessage();

            if (isStudentSender && isTutorReceiver)
            {
                message = new PrivateMessage
                {
                    StudentSenderId = senderId,
                    TutorReceiverId = receiverId,
                    MessageContent = content,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Status = Enums.MessageStatuses.Sent
                };
            }
            else
            {
                message = new PrivateMessage
                {
                    TutorSenderId = senderId,
                    StudentReceiverId = receiverId,
                    MessageContent = content,
                    Timestamp = DateTime.Now,
                    IsRead = false,
                    Status = Enums.MessageStatuses.Sent
                };
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<bool> MarkAsReadAsync(Guid messageId, Guid userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId &&
                    (m.StudentReceiverId == userId || m.TutorReceiverId == userId));

            if (message != null && !message.IsRead)
            {
                message.MarkAsRead();
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<ConversationPreview>> GetRecentConversationsAsync(Guid userId)
        {
            var userMessages = await _context.Messages
                .Where(m => (m.StudentSenderId == userId || m.TutorSenderId == userId ||
                           m.StudentReceiverId == userId || m.TutorReceiverId == userId) &&
                           !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToListAsync();

            var conversations = new Dictionary<Guid, ConversationPreview>();

            foreach (var message in userMessages)
            {
                Guid otherUserId = Guid.Empty;
                string otherUserName = "Unknown User";

                if (message.StudentSenderId == userId)
                {
                    otherUserId = message.TutorReceiverId ?? Guid.Empty;
                    var tutor = await _context.Tutors.FindAsync(otherUserId);
                    otherUserName = tutor?.Name ?? "Tutor";
                }
                else if (message.TutorSenderId == userId)
                {
                    otherUserId = message.StudentReceiverId ?? Guid.Empty;
                    var student = await _context.Students.FindAsync(otherUserId);
                    otherUserName = student?.Name ?? "Student";
                }
                else if (message.StudentReceiverId == userId)
                {
                    otherUserId = message.TutorSenderId ?? Guid.Empty;
                    var tutor = await _context.Tutors.FindAsync(otherUserId);
                    otherUserName = tutor?.Name ?? "Tutor";
                }
                else if (message.TutorReceiverId == userId)
                {
                    otherUserId = message.StudentSenderId ?? Guid.Empty;
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
                        LastMessage = message.MessageContent,
                        LastMessageTime = message.Timestamp,
                        UnreadCount = 0
                    };
                }

                if ((message.StudentReceiverId == userId || message.TutorReceiverId == userId) && !message.IsRead)
                {
                    conversations[otherUserId].UnreadCount++;
                }
            }

            return conversations.Values
                .OrderByDescending(c => c.LastMessageTime)
                .Take(20)
                .ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Messages
                .Where(m => (m.StudentReceiverId == userId || m.TutorReceiverId == userId) &&
                           !m.IsRead && !m.IsDeleted)
                .CountAsync();
        }

        public async Task<List<object>> SearchUsersAsync(string query)
        {
            var students = await _context.Students
                .Where(s => s.Name.Contains(query) || s.Email.Contains(query))
                .Select(s => new { Id = s.Id, Name = s.Name, Email = s.Email, Type = "Student" })
                .ToListAsync();

            var tutors = await _context.Tutors
                .Where(t => t.Name.Contains(query) || t.Email.Contains(query))
                .Select(t => new { Id = t.Id, Name = t.Name, Email = t.Email, Type = "Tutor" })
                .ToListAsync();

            var results = students.Cast<object>().Concat(tutors.Cast<object>()).ToList();
            return results;
        }
    }
}
