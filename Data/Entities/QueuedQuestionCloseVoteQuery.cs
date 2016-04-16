using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class QueuedQuestionCloseVoteQuery
    {
        [Key]
        public int QueuedQuestionCloseVoteQueryId { get; set; }

        public int QuestionId { get; set; }
    }
}
