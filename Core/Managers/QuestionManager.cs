using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scrapers.API;
using Core.Scrapers.Models;
using Dapper;
using Data;

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

        public static void QueryQueuedQuestions()
        {
            IList<int> questionIds;
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
            {
                questionIds = con.Query<int>(@"
SELECT DISTINCT TOP 100 QuestionID, LastUpdated 
FROM QueuedQuestionQueries
LEFT JOIN Questions on QueuedQuestionQueries.QuestionId < Questions.Id
WHERE Questions.Id IS NULL
OR Questions.LastUpdated < @fiveMinAgo
", new { fiveMinAgo = DateTime.UtcNow.AddMinutes(-5) })
.ToList();
                if (!questionIds.Any())
                    return;

                con.Execute($@"
DELETE FROM QueuedQuestionQueries WHERE QuestionID IN ({string.Join(",", questionIds)})
");
            }
            var questionModels = StackExchangeAPI.GetQuestions(questionIds);
            foreach (var questionModel in questionModels)
                UpsertQuestionInformation(questionModel);
        }

        public static void QueryQueuedCloseVotes()
        {
            IList<int> questionIds;
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
            {
                questionIds = con.Query<int>(@"
SELECT DISTINCT TOP 100 QuestionID, LastUpdated 
FROM QueuedQuestionCloseVoteQueries
LEFT JOIN Questions on QueuedQuestionCloseVoteQueries.QuestionId < Questions.Id
WHERE Questions.Id IS NULL
OR Questions.LastUpdated < @fiveMinAgo
", new { fiveMinAgo = DateTime.UtcNow.AddMinutes(-5) })
.ToList();
                if (!questionIds.Any())
                    return;

                con.Execute($@"
DELETE FROM QueuedQuestionCloseVoteQueries WHERE QuestionID IN ({string.Join(",", questionIds)})
");
            }
            foreach (var questionId in questionIds)
            {
                var questionVoteInfo = StackExchangeAPI.GetQuestionVotes(questionId);
                UpsertQuestionCloseVoteInformation(questionId, questionVoteInfo);
            }
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            using (var context = new ReadWriteDataContext())
            {
                var connection = context.Database.Connection;
                connection.Open();

                using (var trans = connection.BeginTransaction())
                {
                    context.Database.UseTransaction(trans);

                    var existingQuestion = context.Questions.FirstOrDefault(q => q.Id == question.Id);
                    
                    connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                    foreach (var tag in question.Tags)
                    {
                        connection.Execute(UPSERT_TAG_SQL, new { tagName = tag }, trans);
                        connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
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
                            || (existingQuestion.ReopenVotes != question.ReopenVotes)
                            )
                        {
                            //Now we mark it as new activity
                            connection.Execute(@"UPDATE QUESTIONS SET LastTimeActive = LastUpdated WHERE Id = @questionId", new { questionId = question.Id }, trans);
                        }
                    }

                    trans.Commit();
                }
            }
        }

        private static void UpsertQuestionCloseVoteInformation(int questionId, IDictionary<int, int> closeVoteInfo)
        {
            using (var context = new ReadWriteDataContext())
            {
                var connection = context.Database.Connection;
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    context.Database.UseTransaction(trans);

                    var existingQuestion = context.Questions.FirstOrDefault(q => q.Id == questionId);
                    var existingVotes = existingQuestion?.CloseVotes
                        .GroupBy(ev => ev.VoteTypeId)
                        .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<int, int>();

                    bool numCloseVotesChanged = false;
                    foreach (var voteGroup in closeVoteInfo)
                    {
                        int numToInsert;
                        if (existingVotes.ContainsKey(voteGroup.Key))
                            numToInsert = voteGroup.Value - existingVotes[voteGroup.Key];
                        else
                            numToInsert = voteGroup.Value;

                        for (var i = 0; i < numToInsert; i++)
                        {
                            connection.Execute(INSERT_QUESTION_VOTE_SQL, new {questionId, voteTypeId = voteGroup.Key }, trans);
                            numCloseVotesChanged = true;
                        }
                    }
                    foreach (var existingVote in existingVotes)
                    {
                        int numToDelete;
                        if (closeVoteInfo.ContainsKey(existingVote.Key))
                            numToDelete = existingVote.Value - closeVoteInfo[existingVote.Key];
                        else
                            numToDelete = existingVote.Value;

                        //Delete newest vote of that type
                        for (var i = 0; i < numToDelete; i++)
                        {
                            connection.Execute(DELETE_NEWEST_VOTE_SQL, new {questionId, voteTypeId = existingVote.Key}, trans);
                            numCloseVotesChanged = true;
                        }
                    }

                    connection.Execute(
                        numCloseVotesChanged
                            ? @"UPDATE QUESTIONS SET LastUpdated = GETUTCDATE(), LastTimeActive = GETUTCDATE WHERE Id = @questionId"
                            : @"UPDATE QUESTIONS SET LastUpdated = GETUTCDATE() WHERE Id = @questionId",
                        new {questionId}, trans);

                    trans.Commit();
                }
            }
        }
    }
}
