using Core;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            var connecter = new StackOverflowConnecter();
            var questions = connecter.GetRecentCloseVoteIds();
        }
    }
}
