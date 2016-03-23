using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using RestSharp;
using Utils;

namespace Core
{
    public class StackOverflowConnecter
    {
        private const string BaseUrl = @"https://stackoverflow.com";

        private readonly StackOverflowAuthenticator _authenticator = new StackOverflowAuthenticator(Configuration.UserName, Configuration.Password);
        private readonly Regex _questionIdRegex = new Regex("\\/questions\\/(?<questionID>\\d+)\\/.*");

        public class RecentCloseVote
        {
            public int QuestionId { get; set; }
            public DateTime DateSeen { get; set; }
            public int NumVotes { get; set; }
            public string VoteType { get; set; }
        }

        public IList<RecentCloseVote> GetRecentCloseVotes()
        {
            var restClient = new RestClient(BaseUrl);

            var restRequest = new RestRequest("tools", Method.GET);
            _authenticator.AuthenticateRequest(restRequest);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", "recentClose");
            
            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var rows = parser.Result.QuerySelectorAll("table tr");
            return rows.Select(r =>
            {
                var reason = r.QuerySelector(".close-reason").TextContent;
                var votes = int.Parse(r.QuerySelector(".cnt").TextContent);

                var link = r.QuerySelector("td a");
                var url = link.GetAttribute("href");

                var match = _questionIdRegex.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);

                return new RecentCloseVote
                {
                    DateSeen = DateTime.Now,
                    NumVotes = votes,
                    QuestionId = id,
                    VoteType = reason
                };
            }).ToList();
        }
    }
}
