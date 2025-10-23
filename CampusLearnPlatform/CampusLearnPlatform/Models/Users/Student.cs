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
        internal string ProfilePictureUrl;

        [Key]
        [Column("student_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("password")]
        public string PasswordHash { get; set; }

        [Column("profile_info")]
        public string ProfileInfo { get; set; }

        // Simple constructor
        public Student()
        {
        }

        // Constructor with basic properties
        public Student(string name, string email, string profileInfo)
        {
            Name = name;
            Email = email;
            ProfileInfo = profileInfo;
        }
    }

}
