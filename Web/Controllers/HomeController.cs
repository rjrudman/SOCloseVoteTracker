using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using Core;
using Core.Workers;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PollQuestion(int questionId)
        {
            Pollers.QueueQuestionQuery(questionId, TimeSpan.FromMinutes(2));
            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public ActionResult Poll(IList<int> questionIds)
        {
            Logger.LogInfo($"Polling questions: {string.Join(",", questionIds)}");
            foreach(var questionId in questionIds)
                Pollers.QueueQuestionQuery(questionId, null, true);

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }
    }
}