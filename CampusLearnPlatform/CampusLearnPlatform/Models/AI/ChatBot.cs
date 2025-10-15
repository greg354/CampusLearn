using CampusLearnPlatform.Models.Communication;

namespace CampusLearnPlatform.Models.AI
{
    public class ChatBot
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }
        public double ConfidenceThreshold { get; set; }
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }

        // Navigation properties
        public virtual ICollection<ChatSession> ChatSessions { get; set; }
        public virtual ICollection<FAQ> FAQs { get; set; }

        public ChatBot()
        {
            IsActive = true;
            LastUpdated = DateTime.UtcNow;
            ConfidenceThreshold = 0.7;
            TotalQueries = 0;
            SuccessfulQueries = 0;
            ChatSessions = new List<ChatSession>();
            FAQs = new List<FAQ>();
        }

        public ChatBot(string name, string version) : this()
        {
            Name = name;
            Version = version;
        }

        // Methods
        public string ProcessQuery(string query)
        {
            TotalQueries++;
            return "I'll help you with that!";
        }

        public bool CanAnswerQuery(string query)
        {
            return !string.IsNullOrWhiteSpace(query) && query.Length > 3;
        }

        public EscalationRequest CreateEscalation(int sessionId, Guid studentId, string query, string module)
        {
            return new EscalationRequest(sessionId, studentId, query, module);
        }

        public void UpdateConfidenceThreshold(double threshold)
        {
            if (threshold >= 0 && threshold <= 1)
            {
                ConfidenceThreshold = threshold;
            }
        }

        public double GetSuccessRate()
        {
            return TotalQueries > 0 ? (double)SuccessfulQueries / TotalQueries : 0;
        }

        public void IncrementSuccessfulQueries()
        {
            SuccessfulQueries++;
        }

        public void UpdateVersion(string newVersion)
        {
            Version = newVersion;
            LastUpdated = DateTime.UtcNow;
        }

        public void ResetStatistics()
        {
            TotalQueries = 0;
            SuccessfulQueries = 0;
        }
    }
}