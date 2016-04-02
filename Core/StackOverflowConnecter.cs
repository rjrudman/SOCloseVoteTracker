using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Core.Models;
using Core.RestRequests;
using Core.Workers;
using Data;
using RestSharp;
using Utils;

namespace Core
{
    public class StackOverflowConnecter
    {
        private const string API_URL = @"https://api.stackexchange.com/2.2";
        private const string SITE_URL = @"https://stackoverflow.com";

        private readonly StackOverflowAuthenticator _authenticator = new StackOverflowAuthenticator(Configuration.UserName, Configuration.Password);
        private readonly Regex _questionIdRegex = new Regex("\\/questions\\/(?<questionID>\\d+)(\\/.*|$)");
        private readonly Regex _undeleteVoteCount = new Regex("^undelete \\((?<numVotes>\\d)\\)$");
        private readonly Regex _deleteVoteCount = new Regex("^delete \\((?<numVotes>\\d)\\)$");

        private IList<int> GetCloseVoteQueue(string mode)
        {
            var throttler = new RestRequestThrottler(SITE_URL, "tools", Method.GET, _authenticator);

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

                var match = _questionIdRegex.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);

                return id;
            }).ToList();
        }

        public IList<int> GetRecentCloseVoteReviews()
        {
            var throttler = new RestRequestThrottler(SITE_URL, "review/close/history", Method.GET, _authenticator);

            var response = throttler.Execute();
            var parser = new HtmlParser(response.Content);

            var rows = parser.Result.QuerySelectorAll(".question-hyperlink");
            return rows.Select(r =>
            {
                var text = r.TextContent;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    int id;
                    if (int.TryParse(text, out id))
                        return id;
                }
                return (int?) null;
            })
                .Where(r => r.HasValue)
                .Select(r => r.Value)
                .ToList();
        }

        public IList<int> GetRecentlyClosed()
        {
            return GetCloseVoteQueue("recentlyClosed");
        }

        public IList<int> GetMostVotedCloseVotesQuestionIds()
        {
            return GetCloseVoteQueue("topClose");
        }

        public IList<int> GetRecentCloseVoteQuestionIds()
        {
            return GetCloseVoteQueue("recentClose");
        }

        public QuestionModel GetQuestionInformation(int questionId)
        {
            var throttler = new RestRequestThrottler(SITE_URL, $"questions/{questionId}", Method.GET, _authenticator);
            var response = throttler.Execute();
            var parser = new HtmlParser(response.Content);
            if (response.StatusCode != HttpStatusCode.OK)
                return null;

            if (!response.ResponseUri.ToString().Contains("stackoverflow.com"))
                return null;

            parser.Parse();

            var idElement = parser.Result.QuerySelector("meta[property='og:url']");
            if (idElement != null)
            {
                var url = idElement.GetAttribute("content");

                var match = _questionIdRegex.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);
                if (id != questionId)
                    throw new Exception($"Question ID returned the wrong question: I queried {questionId} but got {id}. URL: {response.ResponseUri}");
            }
            else
            {
                throw new Exception("Page not found");
            }

            var tags = parser.Result.QuerySelectorAll(".post-taglist .post-tag").Select(t => t.TextContent);
            var title = parser.Result.QuerySelector(".question-hyperlink").TextContent;
            var isClosed =
                parser.Result.QuerySelectorAll(".question-status b")
                    .Select(e => e.TextContent)
                    .Any(c => c == "put on hold" || c == "marked");

            var isDeleted = parser.Result.QuerySelectorAll(".question-status b").Select(e => e.TextContent).Any(c => c == "deleted");

            var deleteVotes = 0;
            var deleteVotesElement = parser.Result.QuerySelector($"#delete-post-{questionId}");
            if (deleteVotesElement != null)
            {
                var content = deleteVotesElement.TextContent;
                var match = _deleteVoteCount.Match(content);
                if (match.Success)
                    deleteVotes = int.Parse(match.Groups["numVotes"].Value);
            }

            var undeleteVotes = 0;
            var undeleteVotesElement = parser.Result.QuerySelector(".deleted-post");
            if (undeleteVotesElement != null)
            {
                var content = undeleteVotesElement.TextContent;
                var match = _undeleteVoteCount.Match(content);
                if (match.Success)
                    undeleteVotes = int.Parse(match.Groups["numVotes"].Value);
            }

            var askedStr = parser.Result.QuerySelector(".postcell .post-signature .relativetime").GetAttribute("title");
            var asked = DateTime.ParseExact(askedStr, "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();

            int? dupeParent = null;
            var possibleDupeTargetLink = parser.Result.QuerySelector(".question-originals-of-duplicate a");
            if (possibleDupeTargetLink != null)
            {
                var url = possibleDupeTargetLink.GetAttribute("href");

                var match = _questionIdRegex.Match(url);
                dupeParent = int.Parse(match.Groups["questionID"].Value);
                Pollers.QueryQuestion(dupeParent.Value, DateTime.Now);
            }
            
            var numCloseVotes = 0;
            var numCloseVotesElement = parser.Result.QuerySelector(".close-question-link[data-isclosed='false'] .existing-flag-count");
            if (!string.IsNullOrWhiteSpace(numCloseVotesElement?.TextContent))
                numCloseVotes = int.Parse(numCloseVotesElement.TextContent);

            var numReopenVotes = 0;
            var numReopenVotesElement = parser.Result.QuerySelector(".close-question-link[data-isclosed='true'] .existing-flag-count");
            if (!string.IsNullOrWhiteSpace(numReopenVotesElement?.TextContent))
                numReopenVotes = int.Parse(numReopenVotesElement.TextContent);

            var requireCloseVoteDetails = false;
            if (!isClosed && numCloseVotes > 0)
            {
                using (var context = new DataContext())
                {
                    var question = context.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null && question.QuestionVotes.Count != numCloseVotes)
                        requireCloseVoteDetails = true;
                }
            }

            var votes = requireCloseVoteDetails
                ? GetCloseVotes(questionId)
                : new Dictionary<int, int>();
            
            return new QuestionModel
            {
                Closed = isClosed,
                Deleted = isDeleted,
                Asked = asked,
                DeleteVotes = deleteVotes,
                UndeleteVotes = undeleteVotes,
                ReopenVotes = numReopenVotes,
                DuplicateParentId = dupeParent,
                Title = title,
                Id = questionId,
                Tags = tags.ToList(),
                CloseVotes = votes
            };
        }

        //One request every 3.5 seconds.
        private static readonly TimeSpanSemaphore CloseVotePopupThrottle = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(3.5));
        private Dictionary<int, int> GetCloseVotes(int questionId)
        {
            var throttler = new RestRequestThrottler(SITE_URL, $"flags/questions/{questionId}/close/popup", Method.GET, _authenticator, CloseVotePopupThrottle);
            
            var response = throttler.Execute();
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var closeVoteTags = parser.Result.QuerySelectorAll(".bounty-indicator-tab[title=\"number of votes already cast\"]");

            var votes = new Dictionary<int, int>();

            foreach (var closeVoteTag in closeVoteTags)
            {
                var topLevelCloseReasonNode = closeVoteTag.ParentElement.QuerySelector("#pane-main span.action-name");
                int closeVoteTypeId;
                if (topLevelCloseReasonNode != null)
                {
                    var topLevelCloseReason = topLevelCloseReasonNode.TextContent;
                    switch (topLevelCloseReason)
                    {
                        case "duplicate of...":
                            closeVoteTypeId = 1000;
                            break;
                        case "off-topic because...":
                            continue;
                        case "unclear what you're asking":
                            closeVoteTypeId = 1001;
                            break;
                        case "too broad":
                            closeVoteTypeId = 1002;
                            break;
                        case "primarily opinion-based":
                            closeVoteTypeId = 1003;
                            break;
                        default:
                            continue;
                    }
                }
                else
                {
                    var offtopicLevelCloseReasonNode = closeVoteTag.ParentElement.ParentElement.QuerySelector("input[name=\"close-as-off-topic-reason\"]");
                    if (offtopicLevelCloseReasonNode == null)
                        continue;

                    closeVoteTypeId = int.Parse(offtopicLevelCloseReasonNode.GetAttribute("value"));
                }
                if (!votes.ContainsKey(closeVoteTypeId))
                    votes[closeVoteTypeId] = 0;

                votes[closeVoteTypeId]+= int.Parse(closeVoteTag.TextContent);
            }

            return votes;
        }
    }
}
