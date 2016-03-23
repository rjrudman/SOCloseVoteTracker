using Core;

namespace SOCloseVoteTracker
{
    class Program
    {
        private const string BaseURL = @"https://stackoverflow.com";
        static void Main(string[] args)
        {
            var connecter = new StackOverflowConnecter();
            connecter.GetRecentlyClosed();
            //GetRecentlyClosed();
        }

        
    }
}
