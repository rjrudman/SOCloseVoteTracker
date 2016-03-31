using System;
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

        [ForeignKey("CloseVoteTypeId")]
        public virtual VoteType CloseVoteType { get; set; }
        public int? CloseVoteTypeId { get; set; }

        [ForeignKey("DuplicateParentId")]
        public virtual Question DuplicateParent { get; set; }
        public int? DuplicateParentId { get; set; }

        public DateTime LastUpdated { get; set; }

        public virtual IList<Tag> Tags { get; set; }

        [InverseProperty("Question")]
        public virtual IList<QuestionVote> QuestionVotes { get; set; }

        [InverseProperty("DuplicateParent")]
        public virtual IList<Question> DuplicateChildren { get; set; }

        [InverseProperty("Question")]
        public virtual IList<CVPlsRequest> CVPlsRequests { get; set; }
    }
}
