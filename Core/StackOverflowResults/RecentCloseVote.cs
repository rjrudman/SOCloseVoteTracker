using System;

namespace Core.StackOverflowResults
{
    public class RecentCloseVote
    {
        public int QuestionId { get; set; }
        public DateTime DateSeen { get; set; }
        public int NumVotes { get; set; }
        public string VoteType { get; set; }
    }
}
