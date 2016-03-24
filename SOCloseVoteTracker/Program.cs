using System;
using System.Threading;
using Core;
using Core.Workers;
using Data;
using Hangfire;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            Pollers.QueryQuestions(new[] { 36191145 , 36191338 });
            //var qi = new StackOverflowConnecter().GetQuestionInformation(36191145); //Closed
            //var qi2 = new StackOverflowConnecter().GetQuestionInformation(36191338); //Close votes

            //GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            //var stopping = false;

            //var t = new Thread(() =>
            //{
            //    RecurringJob.AddOrUpdate(() => Pollers.PollCloseVotes(), Cron.Minutely);

            //});
            //t.Start();

            //using (new BackgroundJobServer())
            //{
            //    while (!stopping)
            //        Thread.Sleep(10000);
            //}
        }

    }
}
