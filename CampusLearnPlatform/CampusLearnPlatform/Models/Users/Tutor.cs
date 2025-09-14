using System;
using System.Collections.Generic;
using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Learning;
using CampusLearnPlatform.Models.Communication;


namespace CampusLearnPlatform.Models.Users
{
    public class Tutor : User
    {
        public string Expertise { get; set; }
        public int YearsExperience { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public bool IsVerified { get; set; }

        
        public virtual ICollection<Module> Modules { get; set; }
        public virtual ICollection<PrivateMessage> ReceivedMessages { get; set; }
        public virtual ICollection<LearningMaterial> CreatedMaterials { get; set; }

        
        public Tutor() : base()
        {
            Role = UserRoles.Tutor;
            Modules = new List<Module>();
            ReceivedMessages = new List<PrivateMessage>();
            CreatedMaterials = new List<LearningMaterial>();
            IsVerified = false;
        }

        public Tutor(string email, string expertise) : base(email, UserRoles.Tutor)
        {
            Expertise = expertise;
        }

      
        public override void UpdateProfile(UserProfile profile)
        {
            Profile = profile;
        }

        public void RespondToQuery(int messageId, string response) { }
        public void UploadMaterial(string title, string filePath, int topicId) { }
        public void ManageTopics() { }
        public void SetAvailability(bool isAvailable) { }
        public List<PrivateMessage> GetPendingQueries()
        {
            return new List<PrivateMessage>();
        }
        public void UpdateExpertise(string newExpertise)
        {
            Expertise = newExpertise;
        }
        public double CalculateAverageRating()
        {
            return AverageRating;
        }
    }
}
