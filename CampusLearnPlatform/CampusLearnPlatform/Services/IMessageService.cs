using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Services
{
    public interface IMessageService
    {
        Task<List<PrivateMessage>> GetConversationAsync(Guid currentUserId, Guid otherUserId);
        Task<PrivateMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content, Guid? replyToMessageId = null);

        // attachments (files/images/voice-notes)
        Task<MessageAttachment> AddAttachmentAsync(Guid messageId, string fileName, string contentType, long size, string storagePath);

        // edits
        Task<bool> EditMessageAsync(Guid messageId, Guid editorId, string newContent);

        // reactions
        Task<bool> AddReactionAsync(Guid messageId, Guid userId, string emoji);
        Task<bool> RemoveReactionAsync(Guid messageId, Guid userId, string emoji);

        // delivery/read
        Task MarkDeliveredAsync(Guid messageId, Guid recipientId);
        Task MarkReadAsync(Guid messageId, Guid recipientId);

        // delete (per user)
        Task<bool> DeleteForUserAsync(Guid messageId, Guid userId);

        // search + inbox
        Task<List<ConversationPreview>> GetRecentConversationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<object>> SearchUsersAsync(string query);
    }
}
