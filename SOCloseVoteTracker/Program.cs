using System.Threading;
using Core.Workers;
using Hangfire;

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
