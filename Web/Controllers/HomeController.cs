using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Core.Workers;
using Hangfire;
using WebGrease.Css.Ast.Selectors;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Poll(int questionId)
        {
            BackgroundJob.Enqueue(() => Pollers.QueryQuestion(questionId, DateTime.Now));
            return new EmptyResult();
        }
    }
}