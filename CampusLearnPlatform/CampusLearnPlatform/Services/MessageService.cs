using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Services
{
    public class MessageService : IMessageService
    {
        private readonly CampusLearnDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MessageService(CampusLearnDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<List<PrivateMessage>> GetConversationAsync(Guid currentUserId, Guid otherUserId)
        {
            var deletedIds = await _context.MessageDeletes
                .Where(d => d.UserId == currentUserId)
                .Select(d => d.MessageId)
                .ToListAsync();

            var messages = await _context.Messages
                .Where(m =>
                    // student ↔ tutor (both ways)
                    (m.StudentSenderId == currentUserId && m.TutorReceiverId == otherUserId) ||
                    (m.TutorSenderId == currentUserId && m.StudentReceiverId == otherUserId) ||
                    (m.StudentReceiverId == currentUserId && m.TutorSenderId == otherUserId) ||
                    (m.TutorReceiverId == currentUserId && m.StudentSenderId == otherUserId) ||

                    // student ↔ student
                    (m.StudentSenderId == currentUserId && m.StudentReceiverId == otherUserId) ||
                    (m.StudentSenderId == otherUserId && m.StudentReceiverId == currentUserId) ||

                    // tutor ↔ tutor
                    (m.TutorSenderId == currentUserId && m.TutorReceiverId == otherUserId) ||
                    (m.TutorSenderId == otherUserId && m.TutorReceiverId == currentUserId)
                )
                .Where(m => !deletedIds.Contains(m.Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return messages;
        }

        public async Task<PrivateMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content, Guid? replyToMessageId = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.");

            var senderIsStudent = await _context.Students.AnyAsync(s => s.Id == senderId);
            var receiverIsStudent = await _context.Students.AnyAsync(s => s.Id == receiverId);
            var senderIsTutor = await _context.Tutors.AnyAsync(t => t.Id == senderId);
            var receiverIsTutor = await _context.Tutors.AnyAsync(t => t.Id == receiverId);

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
            else if (senderIsStudent && receiverIsStudent)
            {
                msg.StudentSenderId = senderId;
                msg.StudentReceiverId = receiverId;
            }
            else if (senderIsTutor && receiverIsTutor)
            {
                msg.TutorSenderId = senderId;
                msg.TutorReceiverId = receiverId;
            }
            else
            {
                msg.StudentSenderId = senderId;
                msg.StudentReceiverId = receiverId;
            }

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            // reply mapping (optional)
            if (replyToMessageId.HasValue)
            {
                _context.MessageReplies.Add(new MessageReply
                {
                    MessageId = msg.Id,
                    ParentMessageId = replyToMessageId.Value
                });
                await _context.SaveChangesAsync();
            }

            // create delivery row for recipient (for sent/seen)
            var recipientId = receiverIsStudent || senderIsTutor ? (msg.StudentReceiverId ?? msg.TutorReceiverId) : (msg.TutorReceiverId ?? msg.StudentReceiverId);
            if (recipientId.HasValue)
            {
                _context.MessageDeliveries.Add(new MessageDelivery
                {
                    MessageId = msg.Id,
                    RecipientId = recipientId.Value,
                    DeliveredAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return msg;
        }

        // ---------- Attachments ----------
        public async Task<MessageAttachment> AddAttachmentAsync(Guid messageId, string fileName, string contentType, long size, string storagePath)
        {
            var att = new MessageAttachment
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                FileName = fileName,
                ContentType = contentType,
                FileSizeBytes = size,
                StoragePath = storagePath,
                CreatedAt = DateTime.UtcNow
            };
            _context.MessageAttachments.Add(att);
            await _context.SaveChangesAsync();
            return att;
        }

        // ---------- Edit ----------
        public async Task<bool> EditMessageAsync(Guid messageId, Guid editorId, string newContent)
        {
            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (msg == null) return false;

            // only the original sender can edit
            var isSender = msg.StudentSenderId == editorId || msg.TutorSenderId == editorId;
            if (!isSender) return false;

            var old = msg.MessageContent;
            msg.MessageContent = (newContent ?? string.Empty).Trim();
            await _context.SaveChangesAsync();

            _context.MessageEdits.Add(new MessageEdit
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                EditorId = editorId,
                OldContent = old,
                NewContent = msg.MessageContent,
                EditedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Reactions ----------
        public async Task<bool> AddReactionAsync(Guid messageId, Guid userId, string emoji)
        {
            var exists = await _context.MessageReactions
                .AnyAsync(r => r.MessageId == messageId && r.UserId == userId && r.Emoji == emoji);
            if (exists) return true;

            _context.MessageReactions.Add(new MessageReaction
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveReactionAsync(Guid messageId, Guid userId, string emoji)
        {
            var r = await _context.MessageReactions
                .FirstOrDefaultAsync(x => x.MessageId == messageId && x.UserId == userId && x.Emoji == emoji);
            if (r == null) return false;

            _context.MessageReactions.Remove(r);
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Delivery / Read ----------
        public async Task MarkDeliveredAsync(Guid messageId, Guid recipientId)
        {
            var row = await _context.MessageDeliveries.FindAsync(messageId, recipientId);
            if (row == null)
            {
                row = new MessageDelivery { MessageId = messageId, RecipientId = recipientId, DeliveredAt = DateTime.UtcNow };
                _context.MessageDeliveries.Add(row);
            }
            else if (row.DeliveredAt == null)
            {
                row.DeliveredAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task MarkReadAsync(Guid messageId, Guid recipientId)
        {
            var row = await _context.MessageDeliveries.FindAsync(messageId, recipientId);
            if (row == null)
            {
                row = new MessageDelivery { MessageId = messageId, RecipientId = recipientId, DeliveredAt = DateTime.UtcNow, ReadAt = DateTime.UtcNow };
                _context.MessageDeliveries.Add(row);
            }
            else
            {
                row.DeliveredAt ??= DateTime.UtcNow;
                row.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        // ---------- Delete (per user) ----------
        public async Task<bool> DeleteForUserAsync(Guid messageId, Guid userId)
        {
            var exists = await _context.MessageDeletes.FindAsync(messageId, userId);
            if (exists != null) return true;
            _context.MessageDeletes.Add(new MessageDelete
            {
                MessageId = messageId,
                UserId = userId,
                DeletedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Inbox ----------
        public async Task<List<ConversationPreview>> GetRecentConversationsAsync(Guid userId)
        {
            var hidden = await _context.MessageDeletes
                .Where(d => d.UserId == userId)
                .Select(d => d.MessageId)
                .ToListAsync();

            var raw = await _context.Messages
                .Where(m =>
                    (m.StudentSenderId == userId || m.StudentReceiverId == userId ||
                     m.TutorSenderId == userId || m.TutorReceiverId == userId) &&
                    !hidden.Contains(m.Id))
                .OrderByDescending(m => m.Timestamp)
                .Select(m => new
                {
                    Message = m,
                    OtherId =
                        m.StudentSenderId == userId ? (m.StudentReceiverId ?? m.TutorReceiverId) :
                        m.StudentReceiverId == userId ? (m.StudentSenderId ?? m.TutorSenderId) :
                        m.TutorSenderId == userId ? (m.TutorReceiverId ?? m.StudentReceiverId) :
                        (m.TutorSenderId ?? m.StudentSenderId)
                })
                .Where(x => x.OtherId != null)
                .ToListAsync();

            var grouped = raw
                .GroupBy(x => x.OtherId!.Value)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    Last = g.OrderByDescending(x => x.Message.Timestamp).First().Message
                })
                .ToList();

            var otherIds = grouped.Select(g => g.OtherUserId).Distinct().ToList();

            var studentNames = await _context.Students
                .Where(s => otherIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var tutorNames = await _context.Tutors
                .Where(t => otherIds.Contains(t.Id))
                .Select(t => new { t.Id, t.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var previews = grouped.Select(g =>
            {
                var name =
                    (studentNames.TryGetValue(g.OtherUserId, out var sname) ? sname :
                    (tutorNames.TryGetValue(g.OtherUserId, out var tname) ? tname : "User"));

                return new ConversationPreview
                {
                    OtherUserId = g.OtherUserId,
                    OtherUserName = name,
                    LastMessage = g.Last.MessageContent,
                    LastMessageTime = g.Last.Timestamp,
                    UnreadCount = 0 // computed later from MessageDeliveries if you add unread UI
                };
            })
            .OrderByDescending(p => p.LastMessageTime)
            .Take(20)
            .ToList();

            return previews;
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
        {
            // If you add unread counts later, count deliveries where ReadAt is null for messages sent to userId.
            return Task.FromResult(0);
        }

        // ---------- Search ----------
        public async Task<List<object>> SearchUsersAsync(string query)
        {
            var q = (query ?? string.Empty).Trim();
            if (q.Length < 2) return new List<object>();

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
