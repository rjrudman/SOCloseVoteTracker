using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Core.Models;
using Core.Sockets;
using Dapper;
using Data;
using Data.Entities;
using Data.Migrations;
using Hangfire;
using Hangfire.SqlServer;

namespace Core.Workers
{
    public static class Pollers
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
INSERT INTO QuestionVotes(QuestionId, VoteTypeId, FirstTimeSeen) VALUES (@questionId, @voteTypeId, GETUTCDATE())
";

        public static void Start()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataContext, Configuration>());
            using (var c = new DataContext())
                c.Database.Initialize(true);

            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME, new SqlServerStorageOptions
            {
                JobExpirationCheckInterval = TimeSpan.FromMinutes(5)
            });
            
            if (!Utils.GlobalConfiguration.DisablePolling)
            {
                //Every 5 minutes
                RecurringJob.AddOrUpdate(() => RecentlyClosed(), "*/5 * * * *");
                RecurringJob.AddOrUpdate(() => QueryRecentCloseVotes(), "*/5 * * * *");
                RecurringJob.AddOrUpdate(() => QueryMostCloseVotes(), "*/5 * * * *");
                RecurringJob.AddOrUpdate(() => GetRecentCloseVoteReviews(), "*/5 * * * *");
                //Every hour
                RecurringJob.AddOrUpdate(() => CheckCVPls(), "0 * * * *");

                //Query this every 15 minutes (except on the hour)
                RecurringJob.AddOrUpdate(() => PollActiveQuestionsFifteenMins(), "15,30,45 * * * *");

                //Query this every hour
                RecurringJob.AddOrUpdate(() => PollActiveQuestionsHour(), "0 * * * *");
                
                Chat.JoinAndWatchRoom(Utils.GlobalConfiguration.ChatRoomID);

                PollFrontPage();
            }
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

                foreach (var questionId in questionIdsToCheck)
                    QueueQuestionQuery(questionId);
            }
        }

        public static void GetRecentCloseVoteReviews()
        {
            var matches = new StackOverflowConnecter().GetRecentCloseVoteReviews();
            using (var con = DataContext.PlainConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    foreach (var match in matches)
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
", new { questionId = match.Key, reviewId = match.Value }, trans);
                    }

                    trans.Commit();
                }
            }
            QueueQuestionQueries(matches.Keys);
        }

        public static void PollActiveQuestionsFifteenMins()
        {
            PollActiveQuestions(TimeSpan.FromMinutes(15));
        }

        public static void PollActiveQuestionsHour()
        {
            PollActiveQuestions(TimeSpan.FromMinutes(60));
        }
        
        public static void PollActiveQuestions(TimeSpan timeLastActive)
        {
            var timeActiveSeconds = timeLastActive.TotalSeconds;
            using (var con = DataContext.PlainConnection())
            {
                var questionIds = con.Query<int>(@"SELECT Id FROM QUESTIONS WHERE (DATEDIFF(DAY, [LastTimeActive], GETUTCDATE()) + DATEDIFF(SECOND, [LastTimeActive], GETUTCDATE())) <= @timeActiveSeconds", new { timeActiveSeconds }).ToList();
                foreach (var questionId in questionIds)
                    QueueQuestionQuery(questionId);
            }
        }
        
        public static void PollFrontPage()
        {
            ActiveQuestionsPoller.Register(question =>
            {
                if (question.SiteBaseHostAddress == "stackoverflow.com")
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
            foreach(var questionId in questionIds)
                QueueQuestionQuery(questionId);
        }

        public static void QueueQuestionQuery(int questionId, TimeSpan? after = null, bool forceEnqueue = false)
        {
            using (var con = DataContext.PlainConnection())
                QueueQuestionQuery(con, questionId, after, forceEnqueue);
        }

        public static void QueueQuestionQuery(IDbConnection con, int questionId, TimeSpan? after = null, bool forceEnqueue = false)
        {
            using (var trans = con.BeginTransaction())
            {
                var newQueueTime = DateTime.Now;
                if (after.HasValue)
                    newQueueTime = newQueueTime.Add(after.Value);

                var nextQueueTime = con.Query<DateTime?>("SELECT MIN(ProcessTime) FROM QueuedQuestionQueries with (XLOCK, ROWLOCK) WHERE QuestionId = @id", new {id = questionId}, trans).FirstOrDefault();
                if (nextQueueTime != null && nextQueueTime.Value > DateTime.Now && nextQueueTime.Value < newQueueTime)
                    return;

                con.Execute("INSERT INTO QueuedQuestionQueries(QuestionId, ProcessTime) VALUES (@id, @newQueueTime)", new {newQueueTime = newQueueTime, id = questionId}, trans);
                trans.Commit();

                if (after == null)
                    BackgroundJob.Enqueue(() => QueryQuestion(questionId, DateTime.Now, forceEnqueue));
                else
                    BackgroundJob.Schedule(() => QueryQuestion(questionId, DateTime.Now, forceEnqueue), after.Value);
            }

        }

        //Backwards compatability for a few days
        public static void QueryQuestion(int questionId, DateTime dateRequested)
        {
            QueryQuestion(questionId, dateRequested, false);
        }

        public static void QueryQuestion(int questionId, DateTime dateRequested, bool forceEnqueue)
        {
            var connecter = new StackOverflowConnecter();
            using (var con = DataContext.PlainConnection())
            {
                using (var trans = con.BeginTransaction())
                {
                    con.Execute("DELETE FROM QueuedQuestionQueries with (XLOCK, ROWLOCK) WHERE QuestionId = @id AND ProcessTime <= @processTime", new { id = questionId, processTime = DateTime.Now }, trans);
                    trans.Commit();
                }

                if (!forceEnqueue)
                {
                    var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
                    var lastUpdated = con.Query<DateTime?>("SELECT LastUpdated FROM Questions WHERE Id = @id", new {id = questionId}).FirstOrDefault();
                    if (lastUpdated != null && lastUpdated.Value >= fiveMinutesAgo)
                        return;
                }
            }

            var question = connecter.GetQuestionInformation(questionId);
            if (question != null)
                UpsertQuestionInformation(question);
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            var newCloseVotesFound = false;
            Question existingQuestion;
            using (var context = new DataContext())
            {
                var connection = context.Database.Connection;
                connection.Open();

                using (var trans = connection.BeginTransaction())
                {
                    context.Database.UseTransaction(trans);

                    existingQuestion = context.Questions.FirstOrDefault(q => q.Id == question.Id);
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
                    || (newCloseVotesFound)
                    || (existingQuestion.ReopenVotes != question.ReopenVotes)
                    )
                {
                    //Now we mark it as new activity
                    using (var con = DataContext.PlainConnection())
                    {
                        con.Execute(@"UPDATE QUESTIONS SET LastTimeActive = LastUpdated WHERE Id = @questionId", new { questionId = question.Id });
                    }
                }
            }
            
        }
    }
}
