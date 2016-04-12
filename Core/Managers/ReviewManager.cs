using Core.Workers;
using Dapper;
using Data;
using StackExchangeScraper;

namespace Core.Managers
{
    public static class ReviewManager
    {
        public static void GetRecentCloseVoteReviews()
        {
            var closeVoteReviews = QuestionScraper.GetRecentCloseVoteReviews();
            using (var con = DataContext.PlainConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    foreach (var closeVote in closeVoteReviews)
                    {
                        //If the question doesn't exist, insert a blank row (we will queue it for scraping later)
                        con.Execute(@"
IF NOT EXISTS (SELECT NULL FROM Questions with (XLOCK, ROWLOCK) WHERE Id = @questionId)
BEGIN
    INSERT INTO Questions(Id, Closed, LastUpdated, Deleted, DeleteVotes, UndeleteVotes, ReopenVotes, LastTimeActive, ReviewId) 
        VALUES (@questionId, 0, GETUTCDATE(), 0, 0, 0, 0, GETUTCDATE(), @reviewId)
END
ELSE
BEGIN
    UPDATE Questions SET ReviewId = @reviewId WHERE Id = @questionId
END
", new { questionId = closeVote.Key, reviewId = closeVote.Value }, trans);
                    }

                    trans.Commit();
                }
            }
            Pollers.QueueQuestionQueries(closeVoteReviews.Keys);
        }
    }
}
