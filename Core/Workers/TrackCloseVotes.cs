using System.Collections.Generic;
using System.Linq;
using Dapper;
using Data;
using Hangfire;

namespace Core.Workers
{
    public static class Pollers
    {
        const string UPSERT_QUESTION_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Questions WHERE Id = @Id)
BEGIN
    INSERT INTO Questions(Id, VoteCount, Closed, Title) VALUES (@Id, @VoteCount, @Closed, @Title)
END
ELSE
BEGIN
    UPDATE Questions
    SET VoteCount = @VoteCount, Closed = @Closed, Title = @Title
    WHERE Id = @Id
END
";
        const string UPSERT_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM Tags WHERE TagName = @tagName)
BEGIN
    INSERT INTO Tags(TagName) VALUES (@tagName)
END
";
        const string UPSERT_QUESTION_TAG_SQL = @"
IF NOT EXISTS (SELECT NULL FROM QuestionTags WHERE QuestionID = @questionID AND TagId = @tagName)
BEGIN
    INSERT INTO QuestionTags(QuestionID, TagId) VALUES (@questionID, @tagName)
END
";


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

                BackgroundJob.Enqueue(() => QueryQuestions(questionIdsToPoll));
            }
        }

        public static void QueryQuestions(IEnumerable<int> questionIds)
        {
            var connecter = new StackOverflowConnecter();
            foreach (var questionId in questionIds)
            {
                var question = connecter.GetQuestionInformation(questionId);
                using (var context = new DataContext())
                {
                    var tags = question.Tags.Select(tt => tt.TagName).ToList();
                    
                    var connection = context.Database.Connection;
                    connection.Open();
                    using (var trans = connection.BeginTransaction())
                    {
                        connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                        foreach (var tag in tags)
                        {
                            connection.Execute(UPSERT_TAG_SQL, new {tagName = tag}, trans);
                            connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
                        }
                        
                        trans.Commit();
                    }
                }
            }
        }
    }
}
