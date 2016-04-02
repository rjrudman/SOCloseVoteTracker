using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using Core.Workers;
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
            Pollers.QueueQuestionQuery(questionId, TimeSpan.FromMinutes(2));
            return Redirect($"http://stackoverflow.com/q/{questionId}");
        }
    }
}