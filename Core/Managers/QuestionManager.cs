using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Data;
using Data.Entities;
using StackExchangeScraper;

namespace Core.Managers
{
    public static class QuestionManager
    {
        const string UPSERT_QUESTION_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Questions with (XLOCK, ROWLOCK) WHERE Id = @Id)
BEGIN
    INSERT INTO Questions(Id, Closed, Deleted, DeleteVotes, UndeleteVotes, ReopenVotes, DuplicateParentId, Asked, Title, LastUpdated, LastTimeActive) VALUES (@Id, @Closed, @Deleted, @DeleteVotes, @UndeleteVotes, @ReopenVotes, @DuplicateParentId, @Asked, @Title, GETUTCDATE(), GETUTCDATE())
END
ELSE
BEGIN
    UPDATE Questions
    SET Closed = @Closed, Deleted = @Deleted, DeleteVotes = @DeleteVotes, UndeleteVotes = @UndeleteVotes, ReopenVotes = @ReopenVotes, DuplicateParentId = @DuplicateParentId, Asked = @Asked, Title = @Title, LastUpdated = GETUTCDATE()
    WHERE Id = @Id
END
";
        const string UPSERT_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Tags with (XLOCK, ROWLOCK) WHERE TagName = @tagName)
BEGIN
    INSERT INTO Tags(TagName) VALUES (@tagName)
END
";
        const string UPSERT_QUESTION_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM QuestionTags with (XLOCK, ROWLOCK) WHERE QuestionID = @questionID AND TagName = @tagName)
BEGIN
    INSERT INTO QuestionTags(QuestionID, TagName) VALUES (@questionID, @tagName)
END
";

        const string INSERT_QUESTION_VOTE_SQL = @"
INSERT INTO CloseVotes(QuestionId, VoteTypeId, FirstTimeSeen) VALUES (@questionId, @voteTypeId, GETUTCDATE())
";

        const string DELETE_NEWEST_VOTE_SQL = @"
WITH q AS
(
	SELECT TOP 1 *
	FROM CloseVotes
	WHERE QuestionId = @questionId AND VoteTypeId = @voteTypeId
	ORDER BY FirstTimeSeen DESC
)
DELETE
FROM q
";

        public static void QueryQuestion(int questionId, bool forceEnqueue)
        {
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    con.Execute("DELETE FROM QueuedQuestionQueries with (XLOCK, ROWLOCK) WHERE QuestionId = @id AND ProcessTime <= @processTime", new { id = questionId, processTime = DateTime.Now }, trans);
                    trans.Commit();
                }

                if (!forceEnqueue)
                {
                    var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
                    var lastUpdated = con.Query<DateTime?>("SELECT LastUpdated FROM Questions WHERE Id = @id", new { id = questionId }).FirstOrDefault();
                    if (lastUpdated != null && lastUpdated.Value >= fiveMinutesAgo)
                        return;
                }
            }

            var question = QuestionScraper.GetQuestionInformation(questionId);
            if (question == null)
                return;

            foreach (var dependency in question.Dependencies)
                QueryQuestion(dependency, false);

            UpsertQuestionInformation(question);
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            var numCloseVotesChanged = false;
            Question existingQuestion;
            using (var context = new ReadWriteDataContext())
            {
                var connection = context.Database.Connection;
                connection.Open();

                using (var trans = connection.BeginTransaction())
                {
                    context.Database.UseTransaction(trans);

                    existingQuestion = context.Questions.FirstOrDefault(q => q.Id == question.Id);
                    var existingVotes = existingQuestion?.CloseVotes
                        .GroupBy(ev => ev.VoteTypeId)
                        .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<int, int>();

                    connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                    foreach (var tag in question.Tags)
                    {
                        connection.Execute(UPSERT_TAG_SQL, new { tagName = tag }, trans);
                        connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
                    }

                    if (question.CloseVotes != null)
                    {
                        foreach (var voteGroup in question.CloseVotes)
                        {
                            int numToInsert;
                            if (existingVotes.ContainsKey(voteGroup.Key))
                                numToInsert = voteGroup.Value - existingVotes[voteGroup.Key];
                            else
                                numToInsert = voteGroup.Value;

                            for (var i = 0; i < numToInsert; i++)
                            {
                                connection.Execute(INSERT_QUESTION_VOTE_SQL, new { questionId = question.Id, voteTypeId = voteGroup.Key }, trans);
                                numCloseVotesChanged = true;
                            }
                        }
                        foreach (var existingVote in existingVotes)
                        {
                            int numToDelete;
                            if (question.CloseVotes.ContainsKey(existingVote.Key))
                                numToDelete = existingVote.Value - question.CloseVotes[existingVote.Key];
                            else
                                numToDelete = existingVote.Value;

                            //Delete newest vote of that type
                            for (var i = 0; i < numToDelete; i++)
                            {
                                connection.Execute(DELETE_NEWEST_VOTE_SQL, new { questionId = question.Id, voteTypeId = existingVote.Key }, trans);
                                numCloseVotesChanged = true;
                            }
                        }
                    }

                    trans.Commit();
                }
            }

            //We mark it as active when:
            //Delete status changed
            //Closed status changed
            //Different amount of (un)Delete votes
            //Different amount of reopen/close votes
            if (existingQuestion != null)
            {
                if (
                    (existingQuestion.Deleted != question.Deleted)
                    || (existingQuestion.Closed != question.Closed)
                    || (existingQuestion.DeleteVotes != question.DeleteVotes)
                    || (existingQuestion.UndeleteVotes != question.UndeleteVotes)
                    || (numCloseVotesChanged)
                    || (existingQuestion.ReopenVotes != question.ReopenVotes)
                    )
                {
                    //Now we mark it as new activity
                    using (var con = ReadWriteDataContext.ReadWritePlainConnection())
                    {
                        con.Execute(@"UPDATE QUESTIONS SET LastTimeActive = LastUpdated WHERE Id = @questionId", new { questionId = question.Id });
                    }
                }
            }

        }
    }
}
