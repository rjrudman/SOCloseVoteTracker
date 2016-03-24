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
        }

    }
}
