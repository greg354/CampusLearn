using System.Text;
using System.Text.Json;

namespace CampusLearnPlatform.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        // Use correct model names that are available
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
        // Alternative models you can try:
        // - "gemini-pro" (most reliable)
        // - "gemini-1.0-pro" 
        // - "gemini-1.5-pro-latest" (if you have access)

        public GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API key not configured");
            _logger = logger;
            _logger.LogInformation("GeminiService initialized with model: gemini-pro");
        }

        public async Task<string> GenerateContentAsync(string prompt, string conversationContext = "")
        {
            try
            {
                // Build the full prompt with context
                var fullPrompt = BuildPrompt(prompt, conversationContext);

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
                        maxOutputTokens = 1024,
                        topP = 0.8,
                        topK = 40
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling Gemini API with prompt length: {Length}", fullPrompt.Length);

                // Make the API request
                var response = await _httpClient.PostAsync($"{GEMINI_API_URL}?key={_apiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

                    // More specific error handling
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return "I'm currently undergoing maintenance. Please try again later or contact a tutor for immediate assistance.";
                    }

                    return "I apologize, but I'm having trouble connecting to my AI service right now. Please try again in a moment.";
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Gemini API response received successfully");

                // Parse the response safely
                var generatedText = ParseGeminiResponse(responseContent);

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    _logger.LogWarning("Failed to parse Gemini response");
                    return "I received an unexpected response format. Please try again.";
                }

                return generatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return "I apologize, but I'm experiencing technical difficulties. Please try asking your question again.";
            }
        }

        private string BuildPrompt(string prompt, string conversationContext)
        {
            var systemPrompt = @"CRITICAL INSTRUCTIONS:
                1. You MUST remember ALL personal details from the conversation context
                2. If the user mentioned their name, USE IT in your response
                3. If the user mentioned what they're studying or need help with, REMEMBER IT
                4. Maintain conversation continuity - don't act like each message is isolated

                You are CampusLearn AI, an academic assistant for Belgium Campus students.
                Provide detailed, educational answers to academic questions.
                For platform questions, be brief and direct.

                CONVERSATION CONTEXT IS PROVIDED BELOW - PAY ATTENTION TO IT!";

            if (string.IsNullOrEmpty(conversationContext) || conversationContext.Contains("No previous conversation"))
            {
                return $"{systemPrompt}\n\nNo previous conversation in this session.\n\nUser: {prompt}";
            }
            else
            {
                return $"{systemPrompt}\n\n{conversationContext}\n\nCurrent user message: {prompt}";
            }
        }

        private string ParseGeminiResponse(string responseContent)
        {
            try
            {
                using var jsonDocument = JsonDocument.Parse(responseContent);
                var root = jsonDocument.RootElement;

                // Check for errors first
                if (root.TryGetProperty("error", out var error))
                {
                    _logger.LogError("Gemini API returned error: {Error}", error.GetRawText());
                    return null;
                }

                // Try multiple possible response structures
                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var text))
                        {
                            return text.GetString()?.Trim();
                        }
                    }
                }

                // Alternative structure
                if (root.TryGetProperty("contents", out var contents) &&
                    contents.ValueKind == JsonValueKind.Array &&
                    contents.GetArrayLength() > 0)
                {
                    var firstContent = contents[0];
                    if (firstContent.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var text))
                        {
                            return text.GetString()?.Trim();
                        }
                    }
                }

                _logger.LogWarning("Unexpected Gemini API response structure: {Response}", responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini API response");
                return null;
            }
        }
    }
}