using CampusLearnPlatform.Enums;
using System;
using System.Collections.Generic;
using CampusLearnPlatform.Enums;
using CampusLearnPlatform.Models.Communication;

namespace CampusLearnPlatform.Models.Users
{
    public abstract class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public UserRoles Role { get; set; }
        public bool IsActive { get; set; }

        public virtual UserProfile Profile { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }

        public User()
        {
            CreatedAt = DateTime.Now;
            IsActive = true;
            Notifications = new List<Notification>();
        }

        public User(string email, UserRoles role) : this()
        {
            Email = email;
            Role = role;
        }

        public abstract void UpdateProfile(UserProfile profile);
        public virtual bool ValidateEmail(string email)
        {
            return !string.IsNullOrEmpty(email) && email.Contains("@");
        }
        public virtual void SendNotification(string message) { }
        public virtual bool ChangePassword(string oldPassword, string newPassword)
        {
            return true;
        }
        public virtual void Login()
        {
            LastLogin = DateTime.Now;
        }
        public virtual void Logout() { }
    }
}
