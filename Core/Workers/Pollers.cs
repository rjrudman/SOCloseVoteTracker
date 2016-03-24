﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;
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
    INSERT INTO Questions(Id, Closed, Title) VALUES (@Id, @Closed, @Title)
END
ELSE
BEGIN
    UPDATE Questions
    SET Closed = @Closed, Title = @Title
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

        const string INSERT_QUESTION_VOTE_SQL = @"
INSERT INTO QuestionVotes(QuestionId, VoteTypeId, FirstTimeSeen) VALUES (@questionId, @voteTypeId, GETDATE())
";



        //Here, we only want to queue up a 'get data for this question' job, nothing else.
        public static void QueryRecentCloseVotes()
        {
            var connecter = new StackOverflowConnecter();
            var questions = connecter.GetRecentCloseVoteQuestionIds();

            var questionIds = questions.Select(q => q.QuestionId).ToList();
            BackgroundJob.Enqueue(() => QueryQuestions(questionIds));
        }

        public static void QueryQuestions(IEnumerable<int> questionIds)
        {
            var connecter = new StackOverflowConnecter();
            foreach (var questionId in questionIds)
            {
                var question = connecter.GetQuestionInformation(questionId);
                UpsertQuestionInformation(question);
            }
        }

        private static void UpsertQuestionInformation(QuestionModel question)
        {
            using (var context = new DataContext())
            {
                var existingVotes = context.Questions
                    .Where(q => q.Id == question.Id)
                    .SelectMany(q => q.QuestionVotes)
                    .GroupBy(ev => ev.VoteTypeId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var connection = context.Database.Connection;
                connection.Open();
                using (var trans = connection.BeginTransaction())
                {
                    connection.Execute(UPSERT_QUESTION_SQL, question, trans);
                    foreach (var tag in question.Tags)
                    {
                        //Todo: Delete old/removed tags? If so, hard or soft delete?
                        connection.Execute(UPSERT_TAG_SQL, new { tagName = tag }, trans);
                        connection.Execute(UPSERT_QUESTION_TAG_SQL, new { questionID = question.Id, tagName = tag }, trans);
                    }

                    foreach (var voteGroup in question.CloseVotes)
                    {
                        int numToInsert;
                        if (existingVotes.ContainsKey(voteGroup.Key))
                            numToInsert = existingVotes[voteGroup.Key] - voteGroup.Value;
                        else
                            numToInsert = voteGroup.Value;

                        for (var i = 0; i < numToInsert; i++)
                            connection.Execute(INSERT_QUESTION_VOTE_SQL, new {questionId = question.Id, voteTypeId = voteGroup.Key}, trans);
                    }

                    trans.Commit();
                }
            }
        }
    }
}