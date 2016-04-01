using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class CVPlsRequest
    {
        [Key]
        public int CVPlsRequestId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }
        public int QuestionId { get; set; }

        public string FullMessage { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
