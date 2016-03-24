using System;
using System.Threading;
using Core.Workers;
using Data;
using Hangfire;
using Hangfire.Server;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            RecurringJob.AddOrUpdate(() => Pollers.QueryRecentCloseVotes(), "*/5 * * * *"); //Every 5 minutes
            RecurringJob.AddOrUpdate(() => Pollers.QueryMostCloseVotes(), "*/5 * * * *"); //Every 5 minutes
            var thread = new Thread(() =>
            {
                using (new BackgroundJobServer())
                {
                    while(true) Thread.Sleep(1000);
                }
            });
            thread.Start();


            Console.ReadLine();

        }

    }
}
