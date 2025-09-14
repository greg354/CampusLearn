namespace CampusLearnPlatform.Models.System
{
    public class APIConnector
    {
        public int Id { get; set; }
        public string APIName { get; set; }
        public string APIKey { get; set; }
        public string BaseURL { get; set; }
        public bool IsActive { get; set; }
        public int RateLimit { get; set; }
        public int UsedRequests { get; set; }
        public DateTime LastUsed { get; set; }
        public string APIVersion { get; set; }

        public APIConnector()
        {
            IsActive = true;
            RateLimit = 1000;
            UsedRequests = 0;
            LastUsed = DateTime.Now;
        }

        public APIConnector(string apiName, string apiKey, string baseUrl) : this()
        {
            APIName = apiName;
            APIKey = apiKey;
            BaseURL = baseUrl;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            UsedRequests++;
            LastUsed = DateTime.Now;
            return true;
        }
        public async Task<bool> SendSMSAsync(string phoneNumber, string message)
        {
            UsedRequests++;
            LastUsed = DateTime.Now;
            return true;
        }
        public async Task<bool> SendWhatsAppAsync(string phoneNumber, string message)
        {
            UsedRequests++;
            LastUsed = DateTime.Now;
            return true;
        }
        public bool TestConnection()
        {
            return IsActive;
        }
        public void ResetRateLimit()
        {
            UsedRequests = 0;
        }
        public bool CanMakeRequest()
        {
            return UsedRequests < RateLimit;
        }
        public void UpdateAPIKey(string newKey)
        {
            APIKey = newKey;
        }
    }
}
