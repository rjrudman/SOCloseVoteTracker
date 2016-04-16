using System;
using System.Collections.Generic;
using System.Linq;
using Core.Managers;
using Core.Scrapers;
using Core.Scrapers.Sockets;
using Dapper;
using Data;

namespace Core.Workers
{
    public static class Pollers
    {
        public static void StartPolling()
        {
            if (!Utils.GlobalConfiguration.DisablePolling)
            {
                //Every 5 minutes
                //RecurringJob.AddOrUpdate(() => RecentlyClosed(), "*/5 * * * *");
                //RecurringJob.AddOrUpdate(() => PollRecentCloseVotes(), "*/5 * * * *");
                //RecurringJob.AddOrUpdate(() => PollMostCloseVotes(), "*/5 * * * *");
                //RecurringJob.AddOrUpdate(() => ReviewManager.GetRecentCloseVoteReviews(), "*/5 * * * *");

                ////Every hour
                //RecurringJob.AddOrUpdate(() => SOCVRManager.CheckCVPls(), "0 * * * *");

                ////Query this every 15 minutes (except on the hour)
                //RecurringJob.AddOrUpdate(() => PollActiveQuestionsFifteenMins(), "15,30,45 * * * *");

                ////Query this every hour (except every four hours)
                //RecurringJob.AddOrUpdate(() => PollActiveQuestionsHour(), "0 1,2,3,5,6,7,9,10,11,13,14,15,17,18,19,21,22,23 * * *");

                ////Every 4 hours (except at midnight)
                //RecurringJob.AddOrUpdate(() => PollActiveQuestionsFourHours(), "0 4,8,12,16,20 * * *");

                ////Every day
                //RecurringJob.AddOrUpdate(() => PollActiveQuestionsDay(), "0 0 * * *");

                ChatroomManager.JoinAndWatchSOCVR();

                PollFrontPage();
            }
        }
        
        public static void PollActiveQuestionsFifteenMins()
        {
            PollActiveQuestions(TimeSpan.FromMinutes(15));
        }

        public static void PollActiveQuestionsHour()
        {
            PollActiveQuestions(TimeSpan.FromMinutes(60));
        }

        public static void PollActiveQuestionsFourHours()
        {
            PollActiveQuestions(TimeSpan.FromHours(4));
        }

        public static void PollActiveQuestionsDay()
        {
            PollActiveQuestions(TimeSpan.FromHours(24));
        }
        
        public static void PollActiveQuestions(TimeSpan timeLastActive)
        {
            var timeActiveSeconds = timeLastActive.TotalSeconds;
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
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
                    QueueQuestionQuery((int)question.ID);
            });
        }

        public static void RecentlyClosed()
        {
            QueueQuestionQueries(SEDataScraper.GetRecentlyClosed());
        }

        public static void PollMostCloseVotes()
        {
            QueueQuestionQueries(SEDataScraper.GetMostVotedCloseVotesQuestionIds());
        }

        public static void PollRecentCloseVotes()
        {
            QueueQuestionQueries(SEDataScraper.GetRecentCloseVoteQuestionIds());
        }

        public static void QueueQuestionQuery(int questionId)
        {
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
            {
                con.Execute(@"
INSERT INTO QueuedQuestionQueries (questionId) VALUES (@questionId)
", new { questionId });
            }
        }

        public static void QueueCloseVoteQuery(int questionId)
        {
            using (var con = ReadWriteDataContext.ReadWritePlainConnection())
            {
                con.Execute(@"
INSERT INTO QueuedQuestionCloseVoteQueries (questionId) VALUES (@questionId)
", new { questionId});
            }
        }

        public static void QueueQuestionQueries(IEnumerable<int> questionIds)
        {
            foreach (var questionId in questionIds)
                QueueQuestionQuery(questionId);
        }

        public static void QueueCloseVoteQueries(IEnumerable<int> questionIds)
        {
            foreach (var questionId in questionIds)
                QueueCloseVoteQuery(questionId);
        }
    }
}
