using System;
using System.Collections.Generic;
using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Communication;


namespace CampusLearnPlatform.Models.Users
{
    public class Student : User
    {
        public string StudentNumber { get; set; }
        public int AcademicYear { get; set; }
        public string Program { get; set; }
        public double average { get; set; }

        public virtual ICollection<Subscriptions> Subscriptions { get; set; }
        public virtual ICollection<Topic> CreatedTopics { get; set; }
        public virtual ICollection<PrivateMessage> SentMessages { get; set; }

        public Student() : base()
        {
            Role = UserRoles.Student;
            Subscriptions = new List<Subscriptions>();
            CreatedTopics = new List<Topic>();
            SentMessages = new List<PrivateMessage>();
        }

        public Student(string email, string studentNumber) : base(email, UserRoles.Student)
        {
            StudentNumber = studentNumber;
        }

     
        public override void UpdateProfile(UserProfile profile)
        {
            Profile = profile;
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
