using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.AI
{
    [Table("escalation_request")]
    public class EscalationRequest
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("session_id")]
        public int SessionId { get; set; }

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [Column("query")]
        public string Query { get; set; } = string.Empty;

        [Column("module")]
        public string Module { get; set; } = string.Empty;

        [Column("priority")]
        public string Priority { get; set; } = "Medium";

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("assigned_tutor_id")]  // FIXED: Use snake_case to match database
        public Guid? AssignedTutorId { get; set; }

        [Column("resolved_at")]  // FIXED: Use snake_case to match database
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties - commented out to avoid circular dependencies
        // public virtual ChatSession? ChatSession { get; set; }
        // public virtual Student? Student { get; set; }

        public EscalationRequest()
        {
            CreatedAt = DateTime.UtcNow;
            Status = "Pending";
            Priority = "Medium";
        }

        public EscalationRequest(int sessionId, Guid studentId, string query, string module)
            : this()
        {
            SessionId = sessionId;
            StudentId = studentId;
            Query = query;
            Module = module;
        }

        // Helper methods
        public void AssignToTutor(Guid tutorId)
        {
            AssignedTutorId = tutorId;
            Status = "Assigned";
        }

        public void Resolve()
        {
            Status = "Resolved";
            ResolvedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            Status = "Cancelled";
        }

        public bool IsResolved()
        {
            return Status == "Resolved";
        }

        public bool IsPending()
        {
            return Status == "Pending";
        }
    }
}