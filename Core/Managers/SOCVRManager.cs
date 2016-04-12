﻿using System;
using System.Linq;
using Core.Workers;
using Data;

namespace Core
{
    public static class SOCVRManager
    {
        public static void CheckCVPls()
        {
            using (var ctx = new DataContext())
            {
                var weekAgo = DateTime.Now.ToUniversalTime().AddDays(-7);
                var questionIdsToCheck =
                    ctx.CVPlsRequests
                        .Where(r => !r.Question.Closed && r.CreatedAt >= weekAgo)
                        .Select(r => r.QuestionId)
                        .ToList();

                foreach (var questionId in questionIdsToCheck)
                    Pollers.QueueQuestionQuery(questionId);
            }
        }
    }
}
