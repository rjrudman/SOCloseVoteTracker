using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Title { get; set; }
        public bool Closed { get; set; }

        public IList<Tag> Tags { get; set; }

        [InverseProperty("Question")]
        public IList<QuestionVote> QuestionVotes { get; set; }
    }
}
