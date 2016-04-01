using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Core.Models;
using Core.Sockets;
using Dapper;
using Data;
using Data.Migrations;
using Hangfire;

namespace Core.Workers
{
    public static class Pollers
    {
        const string UPSERT_QUESTION_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Questions with (XLOCK, ROWLOCK) WHERE Id = @Id)
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
IF NOT EXISTS (SELECT NULL FROM Tags with (XLOCK, ROWLOCK) WHERE TagName = @tagName)
BEGIN
    INSERT INTO Tags(TagName) VALUES (@tagName)
END
";
        const string UPSERT_QUESTION_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM QuestionTags with (XLOCK, ROWLOCK) WHERE QuestionID = @questionID AND TagId = @tagName)
BEGIN
    INSERT INTO QuestionTags(QuestionID, TagId) VALUES (@questionID, @tagName)
END
";

        const string INSERT_QUESTION_VOTE_SQL = @"
INSERT INTO QuestionVotes(QuestionId, VoteTypeId, FirstTimeSeen) VALUES (@questionId, @voteTypeId, GETUTCDATE())
";

        public static void Start()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataContext, Configuration>());
            using (var c = new DataContext())
                c.Database.Initialize(true);

            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            //Every 5 minutes
            RecurringJob.AddOrUpdate(() => RecentlyClosed(), "*/5 * * * *");
            RecurringJob.AddOrUpdate(() => QueryRecentCloseVotes(), "*/5 * * * *");
            RecurringJob.AddOrUpdate(() => QueryMostCloseVotes(), "*/5 * * * *");
            RecurringJob.AddOrUpdate(() => GetRecentCloseVoteReviews(), "*/5 * * * *");
            //Every hour
            RecurringJob.AddOrUpdate(() => CheckCVPls(), "0 * * * *");

            PollFrontPage();

            Chat.JoinAndWatchRoom(Utils.Configuration.ChatRoomURL);
        }

        public static void CheckCVPls()
        {
            using (var ctx = new DataContext())
            {
                var weekAgo = DateTime.Now.ToUniversalTime().AddDays(-7);
                var questionIdsToCheck =
                    ctx.CVPlsRequests
                        .Where(r => !r.Question.Closed && r.CreatedAt >= weekAgo)
                        .Select(r => r.QuestionId)
                        .ToList();

                var now = DateTime.Now;
                foreach (var questionId in questionIdsToCheck)
                    QueueQuestionQuery(questionId);
            }
        }

        public static void GetRecentCloseVoteReviews()
        {
            QueueQuestionQueries(new StackOverflowConnecter().GetRecentCloseVoteReviews());
        }

        public static void PollFrontPage()
        {
            ActiveQuestionsPoller.Register(question =>
            {
                QueueQuestionQuery((int) question.ID, TimeSpan.FromMinutes(15));
            });
        }

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
                QueueQuestionQuery(questionId);
        }

        public static void QueueQuestionQuery(int questionId, TimeSpan? after = null)
        {
            using (var con = DataContext.PlainConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    var alreadyQueued = con.Query<bool>("SELECT IsQueued FROM Questions with (XLOCK, ROWLOCK) WHERE Id = @id", new {id = questionId}, trans).FirstOrDefault();
                    if (alreadyQueued)
                        return;

                    con.Execute("UPDATE Questions SET IsQueued = 1 WHERE Id = @id", new {id = questionId}, trans);

                    if (after == null)
                        BackgroundJob.Enqueue(() => QueryQuestion(questionId, DateTime.Now));
                    else
                        BackgroundJob.Schedule(() => QueryQuestion(questionId, DateTime.Now), after.Value);
                }
            }
        }

        public static void QueryQuestion(int questionId, DateTime dateRequested)
        {
            var connecter = new StackOverflowConnecter();
            using (var con = DataContext.PlainConnection())
                con.Execute("UPDATE Questions SET IsQueued = 0 WHERE Id = @id", new { id = questionId });                    
            
            var question = connecter.GetQuestionInformation(questionId);
            if (question != null)
                UpsertQuestionInformation(question);
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            using (var context = new DataContext())
            {
                var connection = context.Database.Connection;
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    context.Database.UseTransaction(trans);

                    var existingQuestion = context.Questions.FirstOrDefault(q => q.Id == question.Id);
                    var existingVotes = existingQuestion?.QuestionVotes
                        .GroupBy(ev => ev.VoteTypeId)
                        .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<int, int>();
                    
                    connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                    foreach (var tag in question.Tags)
                    {
                        //Todo: Delete old/removed tags? If so, hard or soft delete?
                        connection.Execute(UPSERT_TAG_SQL, new { tagName = tag }, trans);
                        connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
                    }

                    var newCloseVotesFound = false;
                    foreach (var voteGroup in question.CloseVotes)
                    {
                        int numToInsert;
                        if (existingVotes.ContainsKey(voteGroup.Key))
                            numToInsert = existingVotes[voteGroup.Key] - voteGroup.Value;
                        else
                            numToInsert = voteGroup.Value;

                        for (var i = 0; i < numToInsert; i++)
                        {
                            connection.Execute(INSERT_QUESTION_VOTE_SQL, new {questionId = question.Id, voteTypeId = voteGroup.Key}, trans);
                            newCloseVotesFound = true;
                        }
                    }

                    //New activity
                    if (newCloseVotesFound || existingQuestion == null)
                        QueueQuestionQuery(question.Id, TimeSpan.FromMinutes(15));
                    else
                    {
                        //No new activity. Keep checking it less and less often, after about a day it will fall off the queue if no new close votes are found.

                        var timeSinceLastUpdated = existingQuestion.LastUpdated - DateTime.Now;
                        if (timeSinceLastUpdated < TimeSpan.FromMinutes(30))
                            QueueQuestionQuery(question.Id, TimeSpan.FromHours(1));
                        else if (timeSinceLastUpdated < TimeSpan.FromHours(1))
                            QueueQuestionQuery(question.Id, TimeSpan.FromHours(2));
                        else if (timeSinceLastUpdated < TimeSpan.FromHours(2))
                            QueueQuestionQuery(question.Id, TimeSpan.FromHours(3));
                        else if (timeSinceLastUpdated < TimeSpan.FromHours(23))
                            QueueQuestionQuery(question.Id, TimeSpan.FromHours(24));
                    }

                    trans.Commit();
                }
            }
        }
    }
}
