namespace CampusLearnPlatform.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateContentAsync(string prompt, string conversationContext = "");
    }
}