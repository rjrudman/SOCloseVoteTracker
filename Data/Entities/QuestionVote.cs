using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class QuestionVote
    {
        [Key]
        public int QuestionVoteId { get; set; }

        [ForeignKey("QuestionId")]
        public Question Question { get; set; }
        public int QuestionId { get; set; }  
        
        [ForeignKey("VoteTypeId")]
        public VoteType VoteType {get; set; }
        public int VoteTypeId { get; set; }

        public DateTime FirstTimeSeen { get; set; }
    }
}
