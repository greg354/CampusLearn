using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("student_module")]
    public class StudentModule
    {
        [Key]
        [Column("student_module_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [Column("module_id")]
        public Guid ModuleId { get; set; }

        [Column("grade")]
        public decimal? Grade { get; set; }
    }
}
