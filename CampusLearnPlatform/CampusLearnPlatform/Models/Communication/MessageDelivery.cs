using System.ComponentModel.DataAnnotations.Schema;

namespace CampusLearnPlatform.Models.Communication
{
    [Table("message_delivery")]
    public class MessageDelivery
    {
        [Column("message_id")]
        public Guid MessageId { get; set; }

        [Column("recipient_id")]
        public Guid RecipientId { get; set; }

        [Column("delivered_at")]
        public DateTime? DeliveredAt { get; set; }

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        public PrivateMessage? Message { get; set; }
    }
}
