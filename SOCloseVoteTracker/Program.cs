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
            var thread = new Thread(() =>
            {
                using (new BackgroundJobServer())
                {
                    while(true) Thread.Sleep(1000);
                }
            });
            thread.Start();

            Pollers.Start();
        }

    }
}
