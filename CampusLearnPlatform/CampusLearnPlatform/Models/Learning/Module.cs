using CampusLearnPlatform.Models.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("module")]
    public class Module
    {
        [Key]
        [Column("module_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        [Column("module_name")]
        public string ModuleName { get; set; }

        [Column("description")]
        public string Description { get; set; }

        public string ModuleCode { get; set; }
        public int Credits { get; set; }
        public int AcademicYear { get; set; }
        public string Semester { get; set; }
        public bool IsActive { get; set; }

        public virtual ICollection<Topic> Topics { get; set; }
        public virtual ICollection<Tutor> Tutors { get; set; }
        public virtual ICollection<Student> Students { get; set; }


        public Module()
        {
            IsActive = true;
            Topics = new List<Topic>();
            Tutors = new List<Tutor>();
            Students = new List<Student>();
        }

    }
}
