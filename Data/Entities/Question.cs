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
        public bool Deleted { get; set; }

        public int? ReviewId { get; set; }

        public int DeleteVotes { get; set; }
        public int UndeleteVotes { get; set; }
        public int ReopenVotes { get; set; }

        public DateTime? LastTimeActive { get; set; }

        public DateTime? Asked { get; set; }

        [ForeignKey("CloseVoteTypeId")]
        public virtual VoteType CloseVoteType { get; set; }
        public int? CloseVoteTypeId { get; set; }

        public int? DuplicateParentId { get; set; }

        public DateTime LastUpdated { get; set; }
        
        public virtual IList<Tag> Tags { get; set; }

        [InverseProperty("Question")]
        public virtual IList<CloseVote> CloseVotes { get; set; }
        
        [InverseProperty("Question")]
        public virtual IList<CVPlsRequest> CVPlsRequests { get; set; }
    }
}
