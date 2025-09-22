using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Communication;
using CampusLearnPlatform.Models.Learning;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CampusLearnPlatform.Models.Users
{
    [Table("tutor")]
    public class Tutor
    {
        [Key]
        [Column("tutor_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } 

        [Column("name")]
        public string Name { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string PasswordHash { get; set; }

        [Column("experience")]
        public string Experience { get; set; }
        public int YearsExperience { get; set; }
        public int TotalRatings { get; set; }
        public bool IsVerified { get; set; }

        
        public virtual ICollection<Module> Modules { get; set; }
        public virtual ICollection<PrivateMessage> ReceivedMessages { get; set; }
        public virtual ICollection<LearningMaterial> CreatedMaterials { get; set; }

        
        public Tutor() : base()
        {
            
            Modules = new List<Module>();
            ReceivedMessages = new List<PrivateMessage>();
            CreatedMaterials = new List<LearningMaterial>();
            IsVerified = false;
        }

        public Tutor(string email, string expertise)
        {
            Experience = expertise;
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
            Experience = newExpertise;
        }
        
    }
}
