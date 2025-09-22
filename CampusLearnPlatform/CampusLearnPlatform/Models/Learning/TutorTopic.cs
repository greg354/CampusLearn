using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Learning
{
    [Table("tutor_topic")]
    public class TutorTopic
    {
        [Key]
        [Column("tutor_topic_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("tutor_id")]
        public Guid TutorId { get; set; }

        [Column("topic_id")]
        public Guid TopicId { get; set; }
    }
}
