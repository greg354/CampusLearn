using System.Text;
using System.Text.Json;

namespace CampusLearnPlatform.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        // Use gemini 2.5-flash model for better performance and cost efficiency
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key not configured");
            _logger = logger;

            _logger.LogInformation($"GeminiService initialized with model:");
        }

        public async Task<string> GenerateContentAsync(string prompt, string conversationContext = "")
        {
            try
            {
                // Build the full prompt with context
                var fullPrompt = string.IsNullOrEmpty(conversationContext)
                    ? $"You are a helpful academic assistant for Belgium Campus students. Answer the following question concisely and helpfully:\n\n{prompt}"
                    : $"You are a helpful academic assistant for Belgium Campus students. Here's the conversation history:\n{conversationContext}\n\nNow answer this question: {prompt}";

                // Prepare the request body
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 500,
                        topP = 0.95
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling Gemini API");

                // Make the API request
                var response = await _httpClient.PostAsync($"{GEMINI_API_URL}?key={_apiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API error: {response.StatusCode} - {errorContent}");

                    // Return a helpful message instead of throwing
                    return "I apologize, but I'm having trouble connecting to my AI service right now. Please try again in a moment, or click 'Contact Tutor' to speak with a human tutor.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Gemini API response received successfully");

                var jsonResponse = JsonDocument.Parse(responseContent);

                // Extract the generated text from the response
                var generatedText = jsonResponse.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return generatedText ?? "I'm sorry, I couldn't generate a proper response.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                // Return graceful error instead of throwing
                return "I apologize, but I'm experiencing technical difficulties. Please try asking your question again, or click 'Contact Tutor' to speak with a human tutor who can help you right away.";
            }
        }
    }
}