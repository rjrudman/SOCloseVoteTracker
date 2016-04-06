using System;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Statuses;

namespace Data.Entities
{
    public class OrderStatusChange
    {
        public int OrderStatusChangeId { get; set; }

        [ForeignKey("Question")]
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }

        public DateTime TimeChanged { get; set; }

        public OrderStatusChangeType ChangeType { get; set; }
    }
}
