using Core.Workers;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main()
        {
            Pollers.QueryQuestions(new[] { 36191338 });
        }

    }
}
