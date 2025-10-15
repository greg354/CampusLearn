using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Models.AI;

namespace CampusLearnPlatform.Models.Communication
{
    public class ChatSession
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsActive { get; set; }
        public string? SessionData { get; set; }  // Made nullable to handle NULL in database
        public int MessageCount { get; set; }
        public bool WasEscalated { get; set; }
        public string? SessionSummary { get; set; }  // Made nullable to handle NULL in database

        public Guid StudentId { get; set; }
        public int ChatbotId { get; set; }

        public virtual Student? Student { get; set; }
        public virtual ChatBot? Chatbot { get; set; }

        public ChatSession()
        {
            StartedAt = DateTime.UtcNow;
            IsActive = true;
            MessageCount = 0;
            WasEscalated = false;
            SessionData = string.Empty;
            SessionSummary = string.Empty;
        }

        public ChatSession(Guid studentId, int chatbotId) : this()
        {
            StudentId = studentId;
            ChatbotId = chatbotId;
        }

        public void StartSession()
        {
            IsActive = true;
            StartedAt = DateTime.UtcNow;
        }

        public void EndSession()
        {
            IsActive = false;
            EndedAt = DateTime.UtcNow;
        }

        public void LogInteraction(string userMessage, string botResponse)
        {
            MessageCount++;
        }

        public void EscalateSession()
        {
            WasEscalated = true;
        }

        public TimeSpan GetSessionDuration()
        {
            return (EndedAt ?? DateTime.UtcNow).Subtract(StartedAt);
        }

        public void AddSummary(string summary)
        {
            SessionSummary = summary;
        }
    }
}