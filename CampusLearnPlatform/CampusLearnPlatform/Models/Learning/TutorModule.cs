using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("tutor_module")]
    public class TutorModule
    {
        [Key]
        [Column("tutor_module_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("tutor_id")]
        public Guid TutorId { get; set; }

        [Column("module_id")]
        public Guid ModuleId { get; set; }
    }
}
