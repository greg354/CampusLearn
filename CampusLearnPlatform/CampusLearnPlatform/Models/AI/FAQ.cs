namespace CampusLearnPlatform.Models.AI
{
    public class FAQ
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public bool IsActive { get; set; }
        public string Keywords { get; set; }


        public int ChatbotId { get; set; }

        public virtual ChatBot Chatbot { get; set; }

        public FAQ()
        {
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            ViewCount = 0;
            IsActive = true;
        }

        public FAQ(string question, string answer, string category, int chatbotId) : this()
        {
            Question = question;
            Answer = answer;
            Category = category;
            ChatbotId = chatbotId;
        }

        public void UpdateAnswer(string newAnswer)
        {
            Answer = newAnswer;
            UpdatedAt = DateTime.Now;
        }
        public void IncrementViewCount()
        {
            ViewCount++;
        }
        public bool MatchesQuery(string query)
        {
            return Question?.ToLower().Contains(query?.ToLower()) == true;
        }
        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.Now;
        }
        public void AddKeywords(string keywords)
        {
            Keywords = keywords;
        }
        public void UpdateCategory(string newCategory)
        {
            Category = newCategory;
            UpdatedAt = DateTime.Now;
        }
    }
}
