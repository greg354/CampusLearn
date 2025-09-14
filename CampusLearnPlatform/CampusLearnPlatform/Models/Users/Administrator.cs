using CampusLearnPlatform.Enums;

namespace CampusLearnPlatform.Models.Users
{
    public class Administrator : User
    {
        public string AdminLevel { get; set; }
        public string Department { get; set; }
        public DateTime LastSystemCheck { get; set; }

        public virtual ICollection<User> ManagedUsers { get; set; }

        public Administrator() : base()
        {
            Role = UserRoles.Administrator;
            ManagedUsers = new List<User>();
            LastSystemCheck = DateTime.Now;
        }

        public Administrator(string email, string adminLevel) : base(email, UserRoles.Administrator)
        {
            AdminLevel = adminLevel;
        }

        public override void UpdateProfile(UserProfile profile)
        {
            Profile = profile;
        }

        public void ManageUser(int userId, string action) { }
        public void ModerateContent(int contentId, bool approve) { }
        public void ViewAnalytics() { }
        public void SystemMaintenance()
        {
            LastSystemCheck = DateTime.Now;
        }
        public void GenerateReports(string reportType) { }
        public void ManagePermissions(int userId, string permission) { }
    }
}
