using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.ViewModels;

namespace CampusLearnPlatform.Services
{
    public interface IMessageService
    {
        Task<List<PrivateMessage>> GetConversationAsync(Guid currentUserId, Guid otherUserId);
        Task<PrivateMessage> SendMessageAsync(Guid senderId, Guid receiverId, string content);
        Task<bool> MarkAsReadAsync(Guid messageId, Guid userId);
        Task<List<ConversationPreview>> GetRecentConversationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<object>> SearchUsersAsync(string query);
    }
}
