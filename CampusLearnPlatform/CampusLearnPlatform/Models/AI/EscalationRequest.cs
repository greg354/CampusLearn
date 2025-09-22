using CampusLearnPlatform.Models.Users;
using System;
using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
namespace CampusLearnPlatform.Models.AI
{
    public class EscalationRequest
    {

        public int Id { get; set; }
        public string Query { get; set; }
        public DateTime CreatedAt { get; set; }
        public EscalationStatus Status { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Resolution { get; set; }
        public Priorities Priority { get; set; }

   
        public int StudentId { get; set; }
        public int? TutorId { get; set; }
        public int ModuleId { get; set; }


        public virtual Student Student { get; set; }
        public virtual Tutor Tutor { get; set; }
        public virtual Module Module { get; set; }

        public EscalationRequest()
        {
            CreatedAt = DateTime.Now;
            Status = EscalationStatus.Pending;
            Priority = Priorities.Medium;
        }

        public EscalationRequest(string query, int studentId, int moduleId) : this()
        {
            Query = query;
            StudentId = studentId;
            ModuleId = moduleId;
        }


        public void AssignToTutor(int tutorId)
        {
            TutorId = tutorId;
            Status = EscalationStatus.Assigned;
            AssignedAt = DateTime.Now;
        }
        public void Resolve(string resolution)
        {
            Status = EscalationStatus.Resolved;
            Resolution = resolution;
            ResolvedAt = DateTime.Now;
        }
        public void SetPriority(Priorities priority)
        {
            Priority = priority;
        }
        public bool IsOverdue()
        {
            return Status == EscalationStatus.Pending && CreatedAt < DateTime.Now.AddHours(-24);
        }
        public void UpdateStatus(EscalationStatus newStatus)
        {
            Status = newStatus;
        }
        public TimeSpan GetWaitTime()
        {
            return (AssignedAt ?? DateTime.Now).Subtract(CreatedAt);
        }
    }
}
