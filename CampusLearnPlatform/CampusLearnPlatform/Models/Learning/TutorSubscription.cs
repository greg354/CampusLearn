using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("tutor_subscription")]
    public class TutorSubscription
    {
        [Key]
        [Column("subscription_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("student_id")]
        public Guid StudentId { get; set; }

        [Column("tutor_id")]
        public Guid TutorId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
