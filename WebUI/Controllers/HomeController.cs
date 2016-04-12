using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Core;
using Dapper;
using Data;
using Data.Entities;
using RestSharp;

namespace WebUI.Controllers
{
    public class HomeController : Controller
    {
        private List<int> HiddenQuestionIds
        {
            get
            {
                var existingList = Session["HiddenQuestionId"] as List<int>;
                if (existingList == null)
                    Session["HiddenQuestionId"] = existingList = new List<int>();

                return existingList;
            }
            set { Session["HiddenQuestionId"] = value; }
        }


        public ActionResult Index()
        {
            return View((SearchQuery)null);
        }

        private void EnqueueQuestionId(int questionId)
        {
            var rc = new RestClient("http://soclosevotetrackerworker.azurewebsites.net");
            var req = new RestRequest("Home/PollQuestion", Method.GET);
            req.AddParameter("questionId", questionId);
            var res = rc.Execute(req);
            if (res.StatusCode != HttpStatusCode.NoContent)
                Logger.LogInfo($"Failed to enqueue question. Code: {res.StatusCode}");
        }

        public ActionResult EnqueueQuestionIds(List<int> questionIds)
        {
            new Thread(() =>
            {
                var rc = new RestClient("http://soclosevotetrackerworker.azurewebsites.net");
                var req = new RestRequest("Home/Poll", Method.POST);
                foreach(var questionId in questionIds)
                    req.AddParameter("questionIds", questionId);
                rc.Execute(req);
            }).Start();
            
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }
        public ActionResult EnqueueAndRedirect(int questionId)
        {
            new Thread(() => EnqueueQuestionId(questionId)).Start();
            return Redirect($"http://stackoverflow.com/q/{questionId}");
        }

        public ActionResult EnqueueAndRedirectReview(int reviewId)
        {
            new Thread(() =>
            {
                using (var con = ReadWriteDataContext.ReadWritePlainConnection())
                {
                    var questionId = con.Query<int?>("SELECT Id from QUESTIONS Where ReviewID = @reviewId", new { reviewId = reviewId }).FirstOrDefault();
                    if (questionId != null)
                        EnqueueQuestionId(questionId.Value);
                }
            }).Start();
            return Redirect($"http://stackoverflow.com/review/close/{reviewId}");
        }
        
        public ActionResult PermaLink(SearchQuery query)
        {
            query.ImmediatelyQuery = true;
            return View("Index", query);
        }

        public class SearchQuery
        {
            public string SortAsc { get; set; }
            public string SortDesc { get; set; }

            public string TagSearch { get; set; }
            public int TagSearchType { get; set; }
            public int Closed { get; set; }
            public int Deleted { get; set; }
            public int HasReview { get; set; }
            public int VoteCount { get; set; }
            public int VoteCountCompare { get; set; }
            public int CloseReason { get; set; }

            public bool ImmediatelyQuery { get; set; }
        }

        public ActionResult ClearHiddenQuestions()
        {
            HiddenQuestionIds = new List<int>();
            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult HideQuestion(int questionId)
        {
            HiddenQuestionIds.Add(questionId);
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public ActionResult RunSQL(string sql)
        {
            try
            {
                using (var con = ReadWriteDataContext.ReadOnlyPlainConnection())
                {
                    var formattedResults = new List<Dictionary<string, object>>();
                    var results = con.Query(sql).ToList();
                    foreach (var row in results)
                    {
                        var formattedRow = new Dictionary<string, object>();
                        foreach (var property in row)
                        {
                            var value = property.Value;
                            if (value != null && value is DateTime)
                            {
                                var dateValue = (DateTime) value;
                                value = dateValue.ToString("yy-MM-dd hh:mm:ss") + " GMT";
                            }
                            formattedRow[property.Key] = value;
                        }
                        formattedResults.Add(formattedRow);
                    }
                    return Json(formattedResults);
                }
            }
            catch (SqlException ex)
            {
                return Json(new[] {new {Error = ex.Message}});
            }
        }

        [HttpPost]
        public ActionResult SearchData(SearchQuery query)
        {
            if (query == null)
                return Json(new object[0]);

            using (var context = new ReadWriteDataContext())
            {
                IQueryable<Question> dataQuery = context.Questions;
                IEnumerable<string> tags = query.TagSearch?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (tags?.Any() ?? false)
                {
                    tags = tags.Select(t => t.Trim());
                    if (query.TagSearchType == 1) //Any of the tags
                        dataQuery = dataQuery.Where(q => q.Tags.Any(t => tags.Contains(t.TagName)));
                    else
                    {
                        foreach(var tag in tags)
                            dataQuery = dataQuery.Where(q => q.Tags.Any(t => t.TagName == tag));
                    }
                }

                if (HiddenQuestionIds.Any())
                    dataQuery = dataQuery.Where(q => !HiddenQuestionIds.Contains(q.Id));

                if (query.Closed == 1)
                    dataQuery = dataQuery.Where(q => !q.Closed);
                else if (query.Closed == 2)
                    dataQuery = dataQuery.Where(q => q.Closed);

                if (query.Deleted == 1)
                    dataQuery = dataQuery.Where(q => !q.Deleted);
                else if (query.Deleted == 2)
                    dataQuery = dataQuery.Where(q => q.Deleted);

                if (query.HasReview == 1)
                    dataQuery = dataQuery.Where(q => q.ReviewId == null);
                else if (query.HasReview == 2)
                    dataQuery = dataQuery.Where(q => q.ReviewId != null);

                if (query.VoteCountCompare == 1)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count == query.VoteCount);
                else if (query.VoteCountCompare == 2)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count != query.VoteCount);
                else if (query.VoteCountCompare == 3)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count < query.VoteCount);
                else if (query.VoteCountCompare == 4)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count <= query.VoteCount);
                else if (query.VoteCountCompare == 5)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count > query.VoteCount);
                else if (query.VoteCountCompare == 6)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Count >= query.VoteCount);

                if (query.CloseReason > 0)
                    dataQuery = dataQuery.Where(q => q.CloseVotes.Any(qv => qv.VoteTypeId == query.CloseReason));

                var selectedQuery = dataQuery
                    .Select(q => new
                    {
                        QuestionId = q.Id,
                        ReviewId = q.ReviewId,
                        Tags = q.Tags,
                        PostLink = q.Title,
                        Status = q.Deleted ? "Deleted" : (q.Closed ? "Closed" : "Open"),
                        LastTimeActive = q.LastTimeActive ?? q.LastUpdated,
                        VoteCount = q.CloseVotes.Count()
                    });

                if (!string.IsNullOrWhiteSpace(query.SortAsc))
                    selectedQuery = ApplyDynamicOrdering(selectedQuery, query.SortAsc, true);
                else if (!string.IsNullOrWhiteSpace(query.SortDesc))
                    selectedQuery = ApplyDynamicOrdering(selectedQuery, query.SortDesc, false);

                selectedQuery = selectedQuery.Take(100);

                var result = selectedQuery
                    .ToList()
                    .Select(q => new
                    {
                        q.QuestionId,
                        q.ReviewId,
                        PostLink = HttpUtility.HtmlEncode(q.PostLink),
                        Tags = string.Join(", ", q.Tags.Select(t => t.TagName)),
                        q.Status,
                        LastTimeActive = q.LastTimeActive.ToString("yy-MM-dd hh:mm:ss") + " GMT",
                        q.VoteCount
                    });

                return Json(result);
            }
        }

        private IOrderedQueryable<TEntityType> ApplyDynamicOrdering<TEntityType>(IQueryable<TEntityType> query, string orderBy, bool asc)
        {
            var param = Expression.Parameter(typeof (TEntityType));
            var expr = Expression.PropertyOrField(param, orderBy);
            var lam = Expression.Lambda(expr, param);
            

            var method = typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == (asc ? "OrderBy" : "OrderByDescending") && m.GetParameters().Length == 2);
            var genericMethod = method.MakeGenericMethod(typeof (TEntityType), expr.Type);
            var result = genericMethod.Invoke(null, new object[] { query, lam }) as IOrderedQueryable<TEntityType>;

            return result;
        }
    }
}