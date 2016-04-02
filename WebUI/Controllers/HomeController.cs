﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using Data;
using Data.Entities;

namespace WebUI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public class SearchQuery
        {
            public string TagSearch { get; set; }
            public int TagSearchType { get; set; }
            public int Closed { get; set; }
            public int VoteCount { get; set; }
            public int VoteCountCompare { get; set; }
            public int CloseReason { get; set; }
        }

        [HttpPost]
        public ActionResult RunSQL(string sql)
        {
            using (var con = DataContext.PlainConnection())
            {
                var formattedResults = new List<Dictionary<string, object>>();
                var results = con.Query(sql).ToList();
                foreach (var row in results)
                {
                    var formattedRow = new Dictionary<string, object>();
                    foreach (var property in row)
                        formattedRow[property.Key] = property.Value;
                    formattedResults.Add(formattedRow);
                }
                return Json(formattedResults);
            }
        }

        [HttpPost]
        public ActionResult SearchData(SearchQuery query)
        {
            if (query == null)
                return Json(new object[0]);

            using (var context = new DataContext())
            {
                IQueryable<Question> dataQuery = context.Questions;
                var tags = query.TagSearch?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (tags?.Any() ?? false)
                {
                    if (query.TagSearchType == 1) //Any of the tags
                        dataQuery = dataQuery.Where(q => q.Tags.Any(t => tags.Contains(t.TagName)));
                    else
                    {
                        foreach(var tag in tags)
                            dataQuery = dataQuery.Where(q => q.Tags.Any(t => t.TagName == tag));
                    }
                }

                if (query.Closed == 1)
                    dataQuery = dataQuery.Where(q => !q.Closed);
                else if (query.Closed == 2)
                    dataQuery = dataQuery.Where(q => q.Closed);

                if (query.VoteCountCompare == 1)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count == query.VoteCount);
                else if (query.VoteCountCompare == 2)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count != query.VoteCount);
                else if (query.VoteCountCompare == 3)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count < query.VoteCount);
                else if (query.VoteCountCompare == 4)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count <= query.VoteCount);
                else if (query.VoteCountCompare == 5)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count > query.VoteCount);
                else if (query.VoteCountCompare == 6)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Count >= query.VoteCount);

                if (query.CloseReason > 0)
                    dataQuery = dataQuery.Where(q => q.QuestionVotes.Any(qv => qv.VoteTypeId == query.CloseReason));

                var result = dataQuery
                    .Select(q => new
                    {
                        q.Id,
                        q.Title,
                        q.Closed, q.LastUpdated,
                        VoteCount = q.QuestionVotes.Count()
                    })
                    .ToList()
                    .Select(q => new
                    {
                        QuestionID = q.Id,
                        q.Title,
                        q.Closed,
                        LastUpdated = q.LastUpdated.ToString("yy-MM-dd hh:mm:ss") + " GMT",
                        q.VoteCount
                    });

                return Json(result);
            }
        }
    }
}