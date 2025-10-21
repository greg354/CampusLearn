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
            // TEMPORARY: Return mock data if database is not connected
            try
            {
                Console.WriteLine($"Attempting to get conversation from database: {currentUserId} -> {otherUserId}");

                // Try to get real data first
                var messages = await _context.Messages
                    .Where(m =>
                        (m.StudentSenderId == currentUserId && m.TutorReceiverId == otherUserId) ||
                        (m.TutorSenderId == currentUserId && m.StudentReceiverId == otherUserId) ||
                        (m.StudentReceiverId == currentUserId && m.TutorSenderId == otherUserId) ||
                        (m.TutorReceiverId == currentUserId && m.StudentSenderId == otherUserId)
                    )
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Timestamp)
                    .ToListAsync();

                Console.WriteLine($"Database returned {messages.Count} messages");
                return messages;
            }
            catch (Exception ex)
            {
                // Fallback to mock data for development
                Console.WriteLine($"Database error in GetConversationAsync: {ex.Message}. Using mock data.");
                return GetMockMessages(currentUserId, otherUserId);
            }
        }

        private List<PrivateMessage> GetMockMessages(Guid currentUserId, Guid otherUserId)
        {
            Console.WriteLine($"Creating mock messages between {currentUserId} and {otherUserId}");

            return new List<PrivateMessage>
            {
                new PrivateMessage
                {
                    Id = Guid.NewGuid(),
                    MessageContent = "Hello! This is a test message.",
                    StudentSenderId = currentUserId,
                    TutorReceiverId = otherUserId,
                    Timestamp = DateTime.Now.AddMinutes(-30),
                    IsRead = true
                },
                new PrivateMessage
                {
                    Id = Guid.NewGuid(),
                    MessageContent = "Hi! Thanks for your message. How can I help you today?",
                    TutorSenderId = otherUserId,
                    StudentReceiverId = currentUserId,
                    Timestamp = DateTime.Now.AddMinutes(-25),
                    IsRead = true
                },
                new PrivateMessage
                {
                    Id = Guid.NewGuid(),
                    MessageContent = "I need help with the programming assignment.",
                    StudentSenderId = currentUserId,
                    TutorReceiverId = otherUserId,
                    Timestamp = DateTime.Now.AddMinutes(-20),
                    IsRead = true
                },
                new PrivateMessage
                {
                    Id = Guid.NewGuid(),
                    MessageContent = "Sure, I can help with that. Which part are you struggling with?",
                    TutorSenderId = otherUserId,
                    StudentReceiverId = currentUserId,
                    Timestamp = DateTime.Now.AddMinutes(-15),
                    IsRead = false
                }
            };
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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Database error in GetRecentConversationsAsync: {ex.Message}. Using mock data.");
                return GetMockConversations(userId);
            }
        }

        private List<ConversationPreview> GetMockConversations(Guid userId)
        {
            return new List<ConversationPreview>
            {
                new ConversationPreview
                {
                    OtherUserId = Guid.NewGuid(),
                    OtherUserName = "John Tutor",
                    LastMessage = "I can help you with that topic",
                    LastMessageTime = DateTime.Now.AddHours(-1),
                    UnreadCount = 1
                },
                new ConversationPreview
                {
                    OtherUserId = Guid.NewGuid(),
                    OtherUserName = "Sarah Student",
                    LastMessage = "Thanks for the help!",
                    LastMessageTime = DateTime.Now.AddDays(-1),
                    UnreadCount = 0
                }
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            try
            {
                return await _context.Messages
                    .Where(m => (m.StudentReceiverId == userId || m.TutorReceiverId == userId) &&
                               !m.IsRead && !m.IsDeleted)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error in GetUnreadCountAsync: {ex.Message}. Using mock count.");
                return 1; // Mock unread count
            }
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
