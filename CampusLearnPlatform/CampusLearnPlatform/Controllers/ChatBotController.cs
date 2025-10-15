using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.AI;
using CampusLearnPlatform.Models.Communication;
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

                //if (userType != "student")
                //{
                //    return Forbid("Only students can use the chatbot");
                //}

                if (!Guid.TryParse(userIdString, out Guid studentId))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message cannot be empty" });
                }

                // Get or create chatbot instance
                var chatbot = await GetOrCreateChatbot();

                // Get or create chat session
                var session = await GetOrCreateSession(studentId, chatbot.Id);

                // Search FAQs first for quick answers
                var faqMatch = await SearchFAQs(request.Message);

                string response;
                bool shouldEscalate = false;

                if (faqMatch != null)
                {
                    // Found a matching FAQ
                    response = faqMatch.Answer;
                    faqMatch.IncrementViewCount();
                    chatbot.IncrementSuccessfulQueries();
                }
                else
                {
                    // Use Gemini API for more complex queries
                    try
                    {
                        // Build context from recent messages
                        var conversationContext = await BuildConversationContext(session.Id);

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

        private async Task<ChatSession> GetOrCreateSession(Guid studentId, int chatbotId)
        {
            // VALIDATION: First, verify the student exists in the database
            var isStudent = await _context.Students.AnyAsync(s => s.Id == studentId);
            var isTutor = await _context.Tutors.AnyAsync(t => t.Id == studentId);

            if (!isStudent && !isTutor)
            {
                _logger.LogError($"User with ID {studentId} not found in database");
                throw new InvalidOperationException($"User with ID {studentId} does not exist. Please ensure you're logged in with a valid account.");
            }

            // Check for existing active session
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.IsActive);

            if (session == null)
            {
                session = new ChatSession(studentId, chatbotId);
                _context.ChatSessions.Add(session);
                await _context.SaveChangesAsync();
            }

            return session;
        }

        private async Task<FAQ?> SearchFAQs(string query)
        {
            var keywords = query.ToLower().Split(' ')
                .Where(w => w.Length > 3)
                .ToList();

            if (!keywords.Any())
                return null;

            var faqs = await _context.FAQs
                .Where(f => f.IsActive)
                .ToListAsync();

            // Simple keyword matching
            foreach (var faq in faqs)
            {
                var questionLower = faq.Question.ToLower();
                if (keywords.Any(k => questionLower.Contains(k)))
                {
                    return faq;
                }
            }

            return null;
        }

        private async Task<string> BuildConversationContext(int sessionId)
        {
            var recentMessages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            if (!recentMessages.Any())
                return string.Empty;

            var context = "Recent conversation:\n";
            foreach (var msg in recentMessages.OrderBy(m => m.CreatedAt))
            {
                context += $"Student: {msg.Message}\nAssistant: {msg.Response}\n";
            }

            return context;
        }

        private bool ShouldEscalateToTutor(string query, string response)
        {
            // Keywords that suggest complex academic questions
            var escalationKeywords = new[]
            {
                "assignment", "project", "exam", "test", "homework",
                "code", "programming", "algorithm", "don't understand",
                "confused", "help me solve", "how do i", "explain",
                "deadline", "submission"
            };

            var queryLower = query.ToLower();
            return escalationKeywords.Any(keyword => queryLower.Contains(keyword));
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