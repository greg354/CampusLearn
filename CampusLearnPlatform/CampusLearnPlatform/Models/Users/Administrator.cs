using CampusLearnPlatform.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Users
{
    [Table("admin")]
    public class Administrator
    {
        [Key]
        [Column("admin_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("password")]
        public string Password { get; set; }
        public string AdminLevel { get; set; }
        public string Department { get; set; }
        public DateTime LastSystemCheck { get; set; }

        public virtual ICollection<User> ManagedUsers { get; set; }

        public Administrator() : base()
        {
           
            ManagedUsers = new List<User>();
            LastSystemCheck = DateTime.Now;
        }

        public Administrator(string email, string adminLevel)
        {
            AdminLevel = adminLevel;
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
