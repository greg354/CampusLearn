using CampusLearnPlatform.Models.Users;
using System;
using System.Collections.Generic;
using CampusLearnPlatform.Models.AI;
namespace CampusLearnPlatform.Models.Communication
{
    public class ChatSession
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsActive { get; set; }
        public string SessionData { get; set; }
        public int MessageCount { get; set; }
        public bool WasEscalated { get; set; }
        public string SessionSummary { get; set; }

        public int StudentId { get; set; }
        public int ChatbotId { get; set; }

        public virtual Student Student { get; set; }
        public virtual Chatbot Chatbot { get; set; }

        public ChatSession()
        {
            StartedAt = DateTime.Now;
            IsActive = true;
            MessageCount = 0;
            WasEscalated = false;
        }

        public ChatSession(int studentId, int chatbotId) : this()
        {
            StudentId = studentId;
            ChatbotId = chatbotId;
        }

        public void StartSession()
        {
            IsActive = true;
            StartedAt = DateTime.Now;
        }
        public void EndSession()
        {
            IsActive = false;
            EndedAt = DateTime.Now;
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
            return (EndedAt ?? DateTime.Now).Subtract(StartedAt);
        }
        public void AddSummary(string summary)
        {
            SessionSummary = summary;
        }
    }
}
