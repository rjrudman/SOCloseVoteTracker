using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class QueuedQuestionQuery
    {
        [Key]
        public int QueuedQuestionQueriesId { get; set; }

        public int QuestionId { get; set; }
    }
}
