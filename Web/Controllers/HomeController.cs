using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Mvc;
using Core.Workers;
using Dapper;
using Data;
using Hangfire;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Poll(IList<int> questionIds)
        {
            foreach(var questionId in questionIds)
                Pollers.QueueQuestionQuery(questionId);

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public ActionResult EnqueueAndRedirect(int questionId)
        {
            new Thread(() => {
                Pollers.QueueQuestionQuery(questionId, TimeSpan.FromMinutes(2), true);
            }).Start();
            return Redirect($"http://stackoverflow.com/q/{questionId}");
        }

        public ActionResult EnqueueAndRedirectReview(int reviewId)
        {
            new Thread(() =>
            {
                using (var con = DataContext.PlainConnection())
                {
                    var questionId = con.Query<int?>("SELECT Id from QUESTIONS Where ReviewID = @reviewId", new {reviewId = reviewId}).FirstOrDefault();
                    if (questionId != null)
                        Pollers.QueueQuestionQuery(questionId.Value, TimeSpan.FromMinutes(2), true);
                }
            }).Start();
            return Redirect($"http://stackoverflow.com/review/close/{reviewId}");
        }
    }
}