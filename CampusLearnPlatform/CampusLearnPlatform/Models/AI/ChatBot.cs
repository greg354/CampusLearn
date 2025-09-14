using CampusLearnPlatform.Models.Communication;

namespace CampusLearnPlatform.Models.AI
{
    public class ChatBot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdated { get; set; }
        public double ConfidenceThreshold { get; set; }
        public int TotalQueries { get; set; }
        public int SuccessfulQueries { get; set; }

        public virtual ICollection<ChatSession> ChatSessions { get; set; }
        public virtual ICollection<FAQ> FAQs { get; set; }

        public ChatBot()
        {
            IsActive = true;
            LastUpdated = DateTime.Now;
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


        public string ProcessQuery(string query)
        {
            TotalQueries++;
            return "I'll help you with that!";
        }
        public bool CanAnswerQuery(string query)
        {
            return query?.Length > 3;
        }
        public EscalationRequest EscalateToTutor(string query, int studentId)
        {
            return new EscalationRequest();
        }
        public void UpdateConfidenceThreshold(double threshold)
        {
            ConfidenceThreshold = threshold;
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
            LastUpdated = DateTime.Now;
        }
    }
}
