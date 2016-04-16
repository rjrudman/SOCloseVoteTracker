
using System.Collections.Generic;
using Core.Scrapers.API;
using Core.Scrapers.Models;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            StackExchangeAPI.GetQuestions(new[] { 36613623 } );
            //new Thread(() =>
            //{
            //    Chat.JoinAndWatchRoom(68414);
            //    while (true) Thread.Sleep(1000);
            //}).Start();
        }
    }
}
