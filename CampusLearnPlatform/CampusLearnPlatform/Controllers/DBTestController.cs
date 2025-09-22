using CampusLearnPlatform.Data;
using CampusLearnPlatform.Models.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusLearnPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DBTestController : ControllerBase
    {
        private readonly CampusLearnDbContext _context;

        public DBTestController(CampusLearnDbContext context)
        {
            _context = context;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    return BadRequest(new { Status = "Cannot connect to database" });
                }

                // Count records in all tables
                var studentCount = await _context.Students.CountAsync();
                var tutorCount = await _context.Tutors.CountAsync();
                var adminCount = await _context.Administrators.CountAsync();
                var moduleCount = await _context.Modules.CountAsync();
                var topicCount = await _context.Topics.CountAsync();
                var materialCount = await _context.LearningMaterials.CountAsync();
                var forumPostCount = await _context.ForumPosts.CountAsync();
                var messageCount = await _context.Messages.CountAsync();
                var notificationCount = await _context.Notifications.CountAsync();
                var studentModuleCount = await _context.StudentModules.CountAsync();
                var tutorModuleCount = await _context.TutorModules.CountAsync();
                var subscriptionCount = await _context.Subscriptions.CountAsync();
                var tutorSubscriptionCount = await _context.TutorSubscriptions.CountAsync();
                var tutorTopicCount = await _context.TutorTopics.CountAsync();

                return Ok(new
                {
                    Status = "Connected successfully to CampusLearn database",
                    TableCounts = new
                    {
                        Students = studentCount,
                        Tutors = tutorCount,
                        Administrators = adminCount,
                        Modules = moduleCount,
                        Topics = topicCount,
                        LearningMaterials = materialCount,
                        ForumPosts = forumPostCount,
                        Messages = messageCount,
                        Notifications = notificationCount,
                        StudentModules = studentModuleCount,
                        TutorModules = tutorModuleCount,
                        Subscriptions = subscriptionCount,
                        TutorSubscriptions = tutorSubscriptionCount,
                        TutorTopics = tutorTopicCount
                    },
                    DatabaseName = "CampusLearn",
                    TotalTables = 14
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Status = "Connection failed",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("create-student")]
        public async Task<IActionResult> CreateSampleStudent()
        {
            try
            {
                var student = new Student
                {
                    Name = "Test Student",
                    Email = "test.student@belgiumcampus.ac.za",
                    PasswordHash = "password123",
                    ProfileInfo = "Test student created by API"
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Status = "Sample student created",
                    StudentId = student.Id,
                    Name = student.Name
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Status = "Failed to create student",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message
                });
            }
        }
    }
}
