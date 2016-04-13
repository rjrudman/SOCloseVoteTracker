
using StackExchangeScraper;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            StackExchangeAPI.GetQuestions(new[] { 36593212 });
            //new Thread(() =>
            //{
            //    Chat.JoinAndWatchRoom(68414);
            //    while (true) Thread.Sleep(1000);
            //}).Start();
        }
    }
}
