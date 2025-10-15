using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.AI;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.Users;
using CampusLearnPlatform.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EscalationRequest = CampusLearnPlatform.Models.AI.EscalationRequest;

namespace CampusLearnPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatBotController : ControllerBase
    {
        private readonly CampusLearnDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<ChatBotController> _logger;

        public ChatBotController(
            CampusLearnDbContext context,
            IGeminiService geminiService,
            ILogger<ChatBotController> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        private bool IsPlatformQuestion(string query)
        {
            var platformKeywords = new[]
            {
                "upload", "material", "topic", "create", "forum", "post",
                "tutor", "register", "profile", "settings", "navigation",
                "how to", "platform", "campuslearn", "website"
            };

            var queryLower = query.ToLower();
            return platformKeywords.Any(keyword => queryLower.Contains(keyword));
        }

        // POST: api/ChatBot/chat
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                // Get user ID from session
                var userIdString = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType");

                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { error = "You must be logged in to use the chatbot" });
                }

                if (!Guid.TryParse(userIdString, out Guid userId))  // Changed from studentId to userId
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                // Get or create chatbot instance
                var chatbot = await GetOrCreateChatbot();

                // Get or create chat session - THIS IS THE LINE TO CHANGE
                var session = await GetOrCreateSession(userId, chatbot.Id);  // Changed from studentId to userId


                // Search FAQs first for quick answers - but only for specific platform questions
                var faqMatch = await SearchFAQs(request.Message);


                string response;
                bool shouldEscalate = false;


                // Only use FAQ for very specific platform-related questions
                if (faqMatch != null && IsPlatformQuestion(request.Message))
                {
                    // Found a matching FAQ for platform questions
                    response = faqMatch.Answer;
                    faqMatch.IncrementViewCount();
                    chatbot.IncrementSuccessfulQueries();
                }
                else
                {
                    // Use Gemini API for academic and general questions
                    try
                    {
                        // Build context from recent messages
                        var conversationContext = await BuildConversationContext(session.Id);

                        // error logging, after building context:
                        _logger.LogInformation("Conversation Context Length: {Length}", conversationContext.Length);
                        _logger.LogInformation("Conversation Context: {Context}", conversationContext);


                        response = await _geminiService.GenerateContentAsync(request.Message, conversationContext);

                        // Check if we should escalate based on keywords or confidence
                        shouldEscalate = ShouldEscalateToTutor(request.Message, response);

                        if (shouldEscalate)
                        {
                            response += "\n\n💡 This seems like a specialized question. Would you like me to connect you with a peer tutor who can provide more detailed assistance?";
                        }

                        chatbot.IncrementSuccessfulQueries();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calling Gemini API");
                        response = "I'm having trouble processing your question right now. Would you like me to connect you with a peer tutor instead?";
                        shouldEscalate = true;
                    }
                }

                // Log the interaction
                session.LogInteraction(request.Message, response);
                session.MessageCount++;

                // Save chat message to database
                var chatMessage = new ChatMessage
                {
                    SessionId = session.Id,
                    Message = request.Message,
                    Response = response,
                    IsFromStudent = true,
                    CreatedAt = DateTime.UtcNow,
                    RequiresEscalation = shouldEscalate
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    response = response,
                    sessionId = session.Id,
                    shouldEscalate = shouldEscalate,
                    messageId = chatMessage.Id,
                    timestamp = chatMessage.CreatedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                // Catch the specific student not found exception
                _logger.LogError(ex, "Student validation failed");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Chat endpoint");
                return StatusCode(500, new { error = "An error occurred while processing your message" });
            }
        }

        // GET: api/ChatBot/debug-sessions
        [HttpGet("debug-sessions")]
        public async Task<IActionResult> DebugSessions()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType");

                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { error = "You must be logged in" });
                }

                if (!Guid.TryParse(userIdString, out Guid userId))
                {
                    return BadRequest(new { error = "Invalid user ID" });
                }

                var sessions = await _context.ChatSessions
                    .Where(s => (userType == "student" && s.StudentId == userId) ||
                               (userType == "tutor" && s.TutorId == userId))
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new
                    {
                        s.Id,
                        s.StartedAt,
                        s.IsActive,
                        s.MessageCount,
                        s.WasEscalated,
                        UserType = s.StudentId != null ? "student" : "tutor"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    currentUserId = userId,
                    userType = userType,
                    totalSessions = sessions.Count,
                    sessions = sessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DebugSessions endpoint");
                return StatusCode(500, new { error = "An error occurred while retrieving sessions" });
            }
        }

        // POST: api/ChatBot/escalate
        [HttpPost("escalate")]
        public async Task<IActionResult> EscalateToTutor([FromBody] EscalationRequestDto escalationDto)
        {
            try
            {
                // Get student ID from session
                var userIdString = HttpContext.Session.GetString("UserId");
                var userType = HttpContext.Session.GetString("UserType");

                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { error = "You must be logged in to escalate" });
                }

                if (userType != "student")
                {
                    return Forbid("Only students can escalate queries");
                }

                if (!Guid.TryParse(userIdString, out Guid studentId))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == escalationDto.SessionId);

                if (session == null)
                {
                    return NotFound(new { error = "Chat session not found" });
                }

                // Verify this session belongs to the logged-in student
                if (session.StudentId != studentId)
                {
                    return Forbid("You can only escalate your own chat sessions");
                }

                // Mark session as escalated
                session.EscalateSession();

                // Create escalation request entity using the proper model
                var escalationRequest = new EscalationRequest(
                    escalationDto.SessionId,
                    studentId, // Use studentId from session
                    escalationDto.Query,
                    escalationDto.Module
                );

                _context.EscalationRequests.Add(escalationRequest);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Your question has been forwarded to a peer tutor. You'll be notified when a tutor responds.",
                    escalationId = escalationRequest.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EscalateToTutor endpoint");
                return StatusCode(500, new { error = "An error occurred while escalating your query" });
            }
        }

        // GET: api/ChatBot/session/{sessionId}
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSession(int sessionId)
        {
            try
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Message,
                        m.Response,
                        m.IsFromStudent,
                        m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new { messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session");
                return StatusCode(500, new { error = "An error occurred while retrieving the session" });
            }
        }

        // GET: api/ChatBot/faqs
        [HttpGet("faqs")]
        public async Task<IActionResult> GetFAQs([FromQuery] string? category = null)
        {
            try
            {
                var query = _context.FAQs.Where(f => f.IsActive);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(f => f.Category == category);
                }

                var faqs = await query
                    .OrderByDescending(f => f.ViewCount)
                    .Take(20)
                    .Select(f => new
                    {
                        f.Id,
                        f.Question,
                        f.Answer,
                        f.Category,
                        f.ViewCount
                    })
                    .ToListAsync();

                return Ok(new { faqs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving FAQs");
                return StatusCode(500, new { error = "An error occurred while retrieving FAQs" });
            }
        }

        // POST: api/ChatBot/end-session
        [HttpPost("end-session")]
        public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
        {
            try
            {
                var session = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId);

                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                session.EndSession();
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Session ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session");
                return StatusCode(500, new { error = "An error occurred while ending the session" });
            }
        }

        // Helper Methods

        private async Task<ChatBot> GetOrCreateChatbot()
        {
            var chatbot = await _context.ChatBots.FirstOrDefaultAsync(c => c.IsActive);

            if (chatbot == null)
            {
                chatbot = new ChatBot("CampusLearn AI Assistant", "1.0");
                _context.ChatBots.Add(chatbot);
                await _context.SaveChangesAsync();
            }

            return chatbot;
        }

        private async Task<ChatSession> GetOrCreateSession(Guid userId, int chatbotId)
        {
            var userType = HttpContext.Session.GetString("UserType");

            // FIRST: Try to find an existing active session for this user
            ChatSession? existingSession = null;

            if (userType == "student")
            {
                existingSession = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.StudentId == userId && s.IsActive);
            }
            else if (userType == "tutor")
            {
                existingSession = await _context.ChatSessions
                    .FirstOrDefaultAsync(s => s.TutorId == userId && s.IsActive);
            }

            // If found, return existing session
            if (existingSession != null)
            {
                _logger.LogInformation("Found existing active session: {SessionId} for user {UserId}",
                    existingSession.Id, userId);
                return existingSession;
            }

            // Otherwise, create new session
            ChatSession newSession;

            if (userType == "student")
            {
                var studentExists = await _context.Students.AnyAsync(s => s.Id == userId);
                if (!studentExists)
                {
                    throw new InvalidOperationException("Student not found");
                }
                newSession = new ChatSession(userId, chatbotId);
            }
            else if (userType == "tutor")
            {
                var tutorExists = await _context.Tutors.AnyAsync(t => t.Id == userId);
                if (!tutorExists)
                {
                    throw new InvalidOperationException("Tutor not found");
                }
                newSession = new ChatSession(chatbotId, userId);
            }
            else
            {
                throw new InvalidOperationException("Invalid user type");
            }

            _context.ChatSessions.Add(newSession);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new session: {SessionId} for user {UserId}",
                newSession.Id, userId);

            return newSession;
        }

        private async Task<FAQ?> SearchFAQs(string query)
        {
            var keywords = query.ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3)
                .Select(w => w.Trim())
                .ToList();

            if (!keywords.Any())
                return null;

            var faqs = await _context.FAQs
                .Where(f => f.IsActive)
                .ToListAsync();

            // Improved matching logic
            foreach (var faq in faqs)
            {
                var questionLower = faq.Question.ToLower();
                var answerLower = faq.Answer.ToLower();

                // Count how many keywords match
                var matchCount = keywords.Count(k =>
                    questionLower.Contains(k) || answerLower.Contains(k));

                // Only return FAQ if at least 2 keywords match and it's not a generic question
                if (matchCount >= 2 && !IsGenericQuestion(query))
                {
                    return faq;
                }
            }

            return null;
        }

        private bool IsGenericQuestion(string query)
        {
            var genericQuestions = new[]
            {
                "what", "how", "why", "when", "where", "explain", "describe",
                "tell me about", "what is", "what are", "can you"
            };

            var queryLower = query.ToLower();
            return genericQuestions.Any(g => queryLower.StartsWith(g));
        }

        private async Task<string> BuildConversationContext(int sessionId)
        {
            try
            {
                var recentMessages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(8)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Building context from {Count} messages for session {SessionId}",
                    recentMessages.Count, sessionId);

                if (!recentMessages.Any())
                {
                    _logger.LogInformation("No recent messages found for session {SessionId}", sessionId);
                    return "No previous conversation in this session.";
                }

                var context = "=== CONVERSATION CONTEXT ===\n";
                context += "IMPORTANT: Remember all personal details mentioned in this conversation.\n";
                context += "If the user mentions their name, programming language, or what they're studying, REMEMBER IT.\n\n";
                context += "Previous messages in this chat:\n";

                foreach (var msg in recentMessages)
                {
                    if (msg.IsFromStudent)
                    {
                        context += $"USER: {msg.Message}\n";

                        // Extract and highlight personal details
                        if (msg.Message.ToLower().Contains("name is"))
                            context += "[NOTE: User mentioned their name here]\n";
                        if (msg.Message.ToLower().Contains("help with") || msg.Message.ToLower().Contains("learning"))
                            context += "[NOTE: User mentioned what they're studying/needing help with]\n";
                    }
                    else
                    {
                        context += $"ASSISTANT: {msg.Response}\n";
                    }
                    context += "---\n";
                }

                context += "\nCURRENT CONVERSATION - REMEMBER THE ABOVE CONTEXT!\n";
                _logger.LogInformation("Built context with {Length} characters", context.Length);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building conversation context for session {SessionId}", sessionId);
                return "Error loading conversation history.";
            }
        }

        private bool ShouldEscalateToTutor(string query, string response)
        {
            // Don't escalate for basic programming help requests
            var basicHelpPhrases = new[]
            {
                "help with", "need help", "explain", "what is", "how to"
            };

            var queryLower = query.ToLower();
            if (basicHelpPhrases.Any(phrase => queryLower.Contains(phrase)))
                return false;

            // Only escalate for complex, assignment-specific requests
            var escalationPhrases = new[]
            {
                "solve this", "debug my", "fix my code", "assignment due",
                "project help", "exam preparation", "complex problem"
            };

            return escalationPhrases.Any(phrase => queryLower.Contains(phrase));
        }
    }

    // Request/Response Models (for API endpoints only)
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? SessionId { get; set; }  // Optional - for continuing existing sessions
    }

    public class EscalationRequestDto
    {
        public int SessionId { get; set; }
        public string Query { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
    }

    public class EndSessionRequest
    {
        public int SessionId { get; set; }
    }

    // Add this model for storing chat messages
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool IsFromStudent { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool RequiresEscalation { get; set; }
    }
}