using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Models;
using Dapper;
using Data;
using Hangfire;

namespace Core.Workers
{
    public static class Pollers
    {
        const string UPSERT_QUESTION_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Questions WHERE Id = @Id)
BEGIN
    INSERT INTO Questions(Id, Closed, Title, LastUpdated) VALUES (@Id, @Closed, @Title, GETUTCDATE())
END
ELSE
BEGIN
    UPDATE Questions
    SET Closed = @Closed, Title = @Title, LastUpdated = GETUTCDATE()
    WHERE Id = @Id
END
";
        const string UPSERT_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Tags WHERE TagName = @tagName)
BEGIN
    INSERT INTO Tags(TagName) VALUES (@tagName)
END
";
        const string UPSERT_QUESTION_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM QuestionTags WHERE QuestionID = @questionID AND TagId = @tagName)
BEGIN
    INSERT INTO QuestionTags(QuestionID, TagId) VALUES (@questionID, @tagName)
END
";

        const string INSERT_QUESTION_VOTE_SQL = @"
INSERT INTO QuestionVotes(QuestionId, VoteTypeId, FirstTimeSeen) VALUES (@questionId, @voteTypeId, GETUTCDATE())
";

        public static void RecentlyClosed()
        {
            QueueQuestionQueries(new StackOverflowConnecter().GetRecentlyClosed());
        }

        public static void QueryMostCloseVotes()
        {
            QueueQuestionQueries(new StackOverflowConnecter().GetMostVotedCloseVotesQuestionIds());
        }

        public static void QueryRecentCloseVotes()
        {
            QueueQuestionQueries(new StackOverflowConnecter().GetRecentCloseVoteQuestionIds());
        }

        private static void QueueQuestionQueries(IEnumerable<int> questionIds)
        {
            var now = DateTime.Now;
            foreach(var questionId in questionIds)
                BackgroundJob.Enqueue(() => QueryQuestion(questionId, now));
        }

        public static void QueryQuestion(int questionId, DateTime dateRequested)
        {
            var connecter = new StackOverflowConnecter();
            using (var context = new DataContext())
            {
                var existingQuestion = context.Questions.FirstOrDefault(q => q.Id == questionId);
                if (existingQuestion != null)
                    //It was already updated after the request was lodged, we can skip this
                    if (existingQuestion.LastUpdated >= dateRequested)
                        return;
            }
            var question = connecter.GetQuestionInformation(questionId);
            UpsertQuestionInformation(question);
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            using (var context = new DataContext())
            {
                var existingVotes = context.Questions
                    .Where(q => q.Id == question.Id)
                    .SelectMany(q => q.QuestionVotes)
                    .GroupBy(ev => ev.VoteTypeId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var connection = context.Database.Connection;
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                    foreach (var tag in question.Tags)
                    {
                        //Todo: Delete old/removed tags? If so, hard or soft delete?
                        connection.Execute(UPSERT_TAG_SQL, new { tagName = tag }, trans);
                        connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
                    }

                    foreach (var voteGroup in question.CloseVotes)
                    {
                        int numToInsert;
                        if (existingVotes.ContainsKey(voteGroup.Key))
                            numToInsert = existingVotes[voteGroup.Key] - voteGroup.Value;
                        else
                            numToInsert = voteGroup.Value;

                        for (var i = 0; i < numToInsert; i++)
                            connection.Execute(INSERT_QUESTION_VOTE_SQL, new {questionId = question.Id, voteTypeId = voteGroup.Key}, trans);
                    }

                    trans.Commit();
                }
            }
        }
    }
}
