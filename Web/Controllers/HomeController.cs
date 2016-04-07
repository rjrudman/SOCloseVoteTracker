using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
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

        public ActionResult Poll(IList<int> questionIds)
        {
            foreach(var questionId in questionIds)
                Pollers.QueueQuestionQuery(questionId);

            return new HttpStatusCodeResult(HttpStatusCode.NoContent);
        }
    }
}