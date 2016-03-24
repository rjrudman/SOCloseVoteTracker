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
            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            var stopping = false;

            var t = new Thread(() =>
            {
                RecurringJob.AddOrUpdate(() => Pollers.PollCloseVotes(), Cron.Minutely);

            });
            t.Start();

            using (new BackgroundJobServer())
            {
                while (!stopping)
                    Thread.Sleep(10000);
            }
        }

    }
}
