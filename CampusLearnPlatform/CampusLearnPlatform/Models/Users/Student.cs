using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.Learning;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CampusLearnPlatform.Models.Users
{
    [Table("student")]
    public class Student
    {
        [Key]
        [Column("student_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("name")]
        public string Name { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string PasswordHash { get; set; }

        [Column("profile_info")]
        public string ProfileInfo { get; set; }

        public int AcademicYear { get; set; }
        public string Program { get; set; }
        public double average { get; set; }

        public virtual ICollection<Subscriptions> Subscriptions { get; set; }
        public virtual ICollection<Topic> CreatedTopics { get; set; }
        public virtual ICollection<PrivateMessage> SentMessages { get; set; }

        public Student() : base()
        {
           
            Subscriptions = new List<Subscriptions>();
            CreatedTopics = new List<Topic>();
            SentMessages = new List<PrivateMessage>();
        }

        public Student(string email, Guid studentNumber) 
        {
            Id = studentNumber;
        }

     
        

        public void CreateTopic(string title, string description, int moduleId) { }
        public void SubscribeToTopic(int topicId) { }
        public void UnsubscribeFromTopic(int topicId) { }
        public void SendMessageToTutor(int tutorId, string message) { }
        public void RateTutor(int tutorId, int rating, string comment) { }
        public List<Topic> GetMyTopics()
        {
            return new List<Topic>();
        }
        public List<PrivateMessage> GetMyMessages()
        {
            return new List<PrivateMessage>();
        }
        public void ParticipateInForum(string content, int moduleId) { }
        public bool CanAccessModule(int moduleId)
        {
            return true;
        }
    }

}
