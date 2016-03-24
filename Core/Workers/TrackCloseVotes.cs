using System.Linq;
using Dapper;
using Data;
using Hangfire;
using MoreLinq;

namespace Core.Workers
{
    public static class Pollers
    {
        //Here, we only want to queue up a 'get data for this question' job, nothing else.
        public static void PollCloseVotes()
        {
            var connecter = new StackOverflowConnecter();
            var questions = connecter.GetRecentCloseVoteIds();

            var questionIds = questions.Select(q => q.QuestionId).ToList();

            using (var con = new DataContext())
            {
                var alreadyExistingQuestions = con.Questions.Where(q => questionIds.Contains(q.Id)).Select(q => q.Id);

                var questionIdsToPoll = questionIds.Except(alreadyExistingQuestions).ToList();

                foreach (var questionIdToPoll in questionIdsToPoll)
                    BackgroundJob.Enqueue(() => QueryQuestion(questionIdToPoll));
            }
        }

        public static void QueryQuestion(int questionId)
        {
            //Hit the page and download complete information about the question.
            var a = 0;

        }
    }
}
