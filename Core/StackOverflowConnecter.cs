using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Data.Entities;
using RestSharp;
using Utils;

namespace Core
{
    public class StackOverflowConnecter
    {
        private const string BaseUrl = @"https://stackoverflow.com";

        private readonly StackOverflowAuthenticator _authenticator = new StackOverflowAuthenticator(Configuration.UserName, Configuration.Password);
        
        public IList<int> GetRecentlyClosed()
        {
            var restClient = new RestClient(BaseUrl);

            var restRequest = new RestRequest("tools", Method.GET);
            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", "recentlyClosed");

            _authenticator.AuthenticateRequest(restRequest);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            var response = restClient.Execute(restRequest);
            var content = response.Content;
            var parser = new HtmlParser(content);
            parser.Parse();
            var rows = parser.Result.QuerySelectorAll("table tr");
            
            var reg = new Regex("\\/questions\\/(?<questionID>\\d+)\\/.*");
            return rows.Select(r =>
            {
                var link = r.QuerySelector("td a");
                var url = link.GetAttribute("href");

                var match = reg.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);
                return id;
            }).ToList();
        }
    }
}
