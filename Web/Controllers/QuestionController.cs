using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Web.Controllers
{
    public class QuestionController : Controller
    {
        [HttpGet]
        public ActionResult GetQuestionInformation(Request request)
        {
            
            var dict = Request.QueryString.Keys.Cast<string>().ToDictionary<string, string, object>(a => a, a => Request.QueryString[a]);

            return Json(new {data = new[] {new[] {"A", "B"}}}, JsonRequestBehavior.AllowGet);
        }
    }

    public class Request
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public IList<ColumnInformation> columns { get; set; } 
    }

    public class ColumnInformation
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool searchable { get; set; }
        public bool Orderable { get; set; }
        public string Value { get; set; }
    }
}