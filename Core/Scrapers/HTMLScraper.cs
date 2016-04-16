using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Core.Scrapers.Authentication;
using Core.Scrapers.Utils;
using RestSharp;
using Utils;

namespace Core.Scrapers
{
    public static class SEDataScraper
    {
        private const string SITE_URL = @"https://stackoverflow.com";

        private static readonly StackOverflowAuthenticator Authenticator = new StackOverflowAuthenticator(GlobalConfiguration.UserName, GlobalConfiguration.Password);
        private static readonly Regex ReviewIdRegex = new Regex("\\/review\\/close\\/(?<reviewID>\\d+)");
        private static readonly Regex QuestionIdRegex = new Regex("\\/questions\\/(?<questionID>\\d+)(\\/.*|$)");
        
        private static IList<int> GetCloseVoteQueue(string mode)
        {
            var throttler = new RestRequestThrottler(SITE_URL, "tools", Method.GET, Authenticator);

            throttler.Request.AddHeader("X-Requested-With", "XMLHttpRequest");

            throttler.Request.AddParameter("tab", "close");
            throttler.Request.AddParameter("daterange", "today");
            throttler.Request.AddParameter("mode", mode);

            var response = throttler.Execute();

            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var rows = parser.Result.QuerySelectorAll(".summary-table tr");
            return rows.Select(r =>
            {
                var link = r.QuerySelector("td a");
                var url = link.GetAttribute("href");

                var match = QuestionIdRegex.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);

                return id;
            }).ToList();
        }

        public static Dictionary<int, int> GetRecentCloseVoteReviews()
        {
            var throttler = new RestRequestThrottler(SITE_URL, "review/close/history", Method.GET, Authenticator);

            var response = throttler.Execute();
            var parser = new HtmlParser(response.Content);

            var rows = parser.Result.QuerySelectorAll(".history-table tr");
            var dict = new Dictionary<int, int>();
            foreach (var row in rows)
            {
                var questionLinkElement = row.QuerySelector(".question-hyperlink");
                var reviewLinkElement = row.QuerySelector("td:nth-child(3) a");

                var questionLinkHref = questionLinkElement.GetAttribute("href");
                var reviewLinkHref = reviewLinkElement.GetAttribute("href");

                var questionMatch = QuestionIdRegex.Match(questionLinkHref);
                var reviewMatch = ReviewIdRegex.Match(reviewLinkHref);
                if (questionMatch.Success && reviewMatch.Success)
                {
                    var questionId = int.Parse(questionMatch.Groups["questionID"].Value);
                    var reviewId = int.Parse(reviewMatch.Groups["reviewID"].Value);

                    dict[questionId] = reviewId;
                }
            }
            return dict;
        }

        public static IList<int> GetRecentlyClosed()
        {
            return GetCloseVoteQueue("recentlyClosed");
        }

        public static IList<int> GetMostVotedCloseVotesQuestionIds()
        {
            return GetCloseVoteQueue("topClose");
        }

        public static IList<int> GetRecentCloseVoteQuestionIds()
        {
            return GetCloseVoteQueue("recentClose");
        }
    }
}
