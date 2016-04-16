//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Net;
//using System.Text.RegularExpressions;
//using AngleSharp.Html;
//using Core.RestRequests;
//using RestSharp;
//using StackExchangeScraper.RestRequests;
//using Utils;

//namespace StackExchangeScraper
//{
//    public static class QuestionScraper
//    {
//        private const string SITE_URL = @"https://stackoverflow.com";

//        private static readonly StackOverflowAuthenticator Authenticator = new StackOverflowAuthenticator(GlobalConfiguration.UserName, GlobalConfiguration.Password);
//        private static readonly Regex ReviewIdRegex = new Regex("\\/review\\/close\\/(?<reviewID>\\d+)");
//        private static readonly Regex QuestionIdRegex = new Regex("\\/questions\\/(?<questionID>\\d+)(\\/.*|$)");
//        private static readonly Regex UndeleteVoteCount = new Regex("^undelete \\((?<numVotes>\\d)\\)$");
//        private static readonly Regex DeleteVoteCount = new Regex("^delete \\((?<numVotes>\\d)\\)$");
//        private static readonly Regex MigratedIdRegex = new Regex("\\/stackoverflow.com\\/posts\\/(?<questionId>\\d+)/revisions");

//        private static IList<int> GetCloseVoteQueue(string mode)
//        {
//            var throttler = new RestRequestThrottler(SITE_URL, "tools", Method.GET, Authenticator);

//            throttler.Request.AddHeader("X-Requested-With", "XMLHttpRequest");

//            throttler.Request.AddParameter("tab", "close");
//            throttler.Request.AddParameter("daterange", "today");
//            throttler.Request.AddParameter("mode", mode);

//            var response = throttler.Execute();

//            var parser = new HtmlParser(response.Content);
//            parser.Parse();

//            var rows = parser.Result.QuerySelectorAll(".summary-table tr");
//            return rows.Select(r =>
//            {
//                var link = r.QuerySelector("td a");
//                var url = link.GetAttribute("href");

//                var match = QuestionIdRegex.Match(url);
//                var id = int.Parse(match.Groups["questionID"].Value);

//                return id;
//            }).ToList();
//        }

//        public static Dictionary<int, int> GetRecentCloseVoteReviews()
//        {
//            var throttler = new RestRequestThrottler(SITE_URL, "review/close/history", Method.GET, Authenticator);

//            var response = throttler.Execute();
//            var parser = new HtmlParser(response.Content);

//            var rows = parser.Result.QuerySelectorAll(".history-table tr");
//            var dict = new Dictionary<int, int>();
//            foreach (var row in rows)
//            {
//                var questionLinkElement = row.QuerySelector(".question-hyperlink");
//                var reviewLinkElement = row.QuerySelector("td:nth-child(3) a");

//                var questionLinkHref = questionLinkElement.GetAttribute("href");
//                var reviewLinkHref = reviewLinkElement.GetAttribute("href");

//                var questionMatch = QuestionIdRegex.Match(questionLinkHref);
//                var reviewMatch = ReviewIdRegex.Match(reviewLinkHref);
//                if (questionMatch.Success && reviewMatch.Success)
//                {
//                    var questionId = int.Parse(questionMatch.Groups["questionID"].Value);
//                    var reviewId = int.Parse(reviewMatch.Groups["reviewID"].Value);

//                    dict[questionId] = reviewId;
//                }
//            }
//            return dict;
//        }

//        public static IList<int> GetRecentlyClosed()
//        {
//            return GetCloseVoteQueue("recentlyClosed");
//        }

//        public static IList<int> GetMostVotedCloseVotesQuestionIds()
//        {
//            return GetCloseVoteQueue("topClose");
//        }

//        public static IList<int> GetRecentCloseVoteQuestionIds()
//        {
//            return GetCloseVoteQueue("recentClose");
//        }

//        public static QuestionModel GetQuestionInformation(int questionId)
//        {
//            return GetQuestionInformation(questionId, 0);
//        }
//        public static QuestionModel GetQuestionInformation(int questionId, int existingVoteCount)
//        {
//            var throttler = new RestRequestThrottler(SITE_URL, $"questions/{questionId}", Method.GET, Authenticator);
//            var response = throttler.Execute();
//            var parser = new HtmlParser(response.Content);
//            if (response.StatusCode != HttpStatusCode.OK)
//                return null;

//            parser.Parse();

//            if (response.ResponseUri.DnsSafeHost != "stackoverflow.com")
//                return GetMigrationQuestionInfo(questionId, parser);

//            var idElement = parser.Result.QuerySelector("meta[property='og:url']");
//            if (idElement != null)
//            {
//                var url = idElement.GetAttribute("content");

//                var match = QuestionIdRegex.Match(url);
//                var id = int.Parse(match.Groups["questionID"].Value);
//                if (id != questionId)
//                    throw new Exception($"Question ID returned the wrong question: I queried {questionId} but got {id}. URL: {response.ResponseUri}");
//            }
//            else
//            {
//                throw new Exception("Page not found");
//            }

//            var tags = parser.Result.QuerySelectorAll(".post-taglist .post-tag").Select(t => t.TextContent);
//            var title = parser.Result.QuerySelector(".question-hyperlink").TextContent;
//            var isClosed =
//                parser.Result.QuerySelectorAll(".question-status b")
//                    .Select(e => e.TextContent)
//                    .Any(c => c == "put on hold" || c == "marked" || c == "closed");

//            var isDeleted = parser.Result.QuerySelectorAll(".question-status b").Select(e => e.TextContent).Any(c => c == "deleted");

//            var deleteVotes = 0;
//            var deleteVotesElement = parser.Result.QuerySelector($"#delete-post-{questionId}");
//            if (deleteVotesElement != null)
//            {
//                var content = deleteVotesElement.TextContent;
//                var match = DeleteVoteCount.Match(content);
//                if (match.Success)
//                    deleteVotes = int.Parse(match.Groups["numVotes"].Value);
//            }

//            var undeleteVotes = 0;
//            var undeleteVotesElement = parser.Result.QuerySelector(".deleted-post");
//            if (undeleteVotesElement != null)
//            {
//                var content = undeleteVotesElement.TextContent;
//                var match = UndeleteVoteCount.Match(content);
//                if (match.Success)
//                    undeleteVotes = int.Parse(match.Groups["numVotes"].Value);
//            }

//            var askedStr = parser.Result.QuerySelector(".postcell .post-signature .relativetime").GetAttribute("title");
//            var asked = DateTime.ParseExact(askedStr, "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();

//            int? dupeParent = null;
//            var dependencies = new List<int>();
//            var possibleDupeTargetLink = parser.Result.QuerySelector(".question-originals-of-duplicate a");
//            if (possibleDupeTargetLink != null)
//            {
//                var url = possibleDupeTargetLink.GetAttribute("href");

//                var match = QuestionIdRegex.Match(url);
//                dupeParent = int.Parse(match.Groups["questionID"].Value);

//                dependencies.Add(dupeParent.Value);
//            }

//            var numCloseVotes = 0;
//            var numCloseVotesElement = parser.Result.QuerySelector(".close-question-link[data-isclosed='false'] .existing-flag-count");
//            if (!string.IsNullOrWhiteSpace(numCloseVotesElement?.TextContent))
//                numCloseVotes = int.Parse(numCloseVotesElement.TextContent);

//            var numReopenVotes = 0;
//            var numReopenVotesElement = parser.Result.QuerySelector(".close-question-link[data-isclosed='true'] .existing-flag-count");
//            if (!string.IsNullOrWhiteSpace(numReopenVotesElement?.TextContent))
//                numReopenVotes = int.Parse(numReopenVotesElement.TextContent);

//            var votes = new Dictionary<int, int>();
//            if (!isClosed && numCloseVotes != 0)
//            {
//                votes = existingVoteCount != numCloseVotes
//                    ? GetCloseVotes(questionId) 
//                    : null;
//            }

//            return new QuestionModel
//            {
//                Closed = isClosed,
//                Deleted = isDeleted,
//                Asked = asked,
//                DeleteVotes = deleteVotes,
//                UndeleteVotes = undeleteVotes,
//                ReopenVotes = numReopenVotes,
//                DuplicateParentId = dupeParent,
//                Title = title,
//                Id = questionId,
//                Tags = tags.ToList(),
//                CloseVotes = votes,
//                Dependencies = dependencies
//            };
//        }

//        private static QuestionModel GetMigrationQuestionInfo(int questionId, HtmlParser parser)
//        {
//            var migrationElement = parser.Result.QuerySelector(".question-status b:contains('migrated')");
//            if (migrationElement == null)
//                return null;
//            var migratedFromElement = migrationElement.ParentElement.QuerySelector("a");

//            var match = MigratedIdRegex.Match(migratedFromElement.GetAttribute("href"));
//            if (!match.Success)
//                return null;

//            if (int.Parse(match.Groups["questionId"].Value) != questionId)
//                return null;

//            var tags = parser.Result.QuerySelectorAll(".post-taglist .post-tag").Select(t => t.TextContent);
//            var title = parser.Result.QuerySelector(".question-hyperlink").TextContent;
//            var askedStr = parser.Result.QuerySelector(".postcell .post-signature .relativetime").GetAttribute("title");
//            var asked = DateTime.ParseExact(askedStr, "yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();

//            return new QuestionModel
//            {
//                Closed = true,
//                Deleted = false,
//                Asked = asked,
//                DeleteVotes = 0,
//                UndeleteVotes = 0,
//                ReopenVotes = 0,
//                DuplicateParentId = null,
//                Title = title,
//                Id = questionId,
//                Tags = tags.ToList(),
//                CloseVotes = new Dictionary<int, int>()
//            };
//        }

//        //One request every 3.5 seconds.
//        private static readonly TimeSpanSemaphore CloseVotePopupThrottle = new TimeSpanSemaphore(1, TimeSpan.FromSeconds(3.5));
//        private static Dictionary<int, int> GetCloseVotes(int questionId)
//        {
//            var throttler = new RestRequestThrottler(SITE_URL, $"flags/questions/{questionId}/close/popup", Method.GET, Authenticator, CloseVotePopupThrottle);

//            var response = throttler.Execute();
//            if (response.StatusCode != HttpStatusCode.OK) //We're throttled to 3 seconds. Exceeding this throttle returns a 409 response. Throwing an exception will put it into the retry queue.
//                throw new Exception("Failed to load close dialog. Status: " + response.StatusCode);

//            var parser = new HtmlParser(response.Content);
//            parser.Parse();

//            var closeVoteTags = parser.Result.QuerySelectorAll(".bounty-indicator-tab[title=\"number of votes already cast\"]");

//            var votes = new Dictionary<int, int>();

//            foreach (var closeVoteTag in closeVoteTags)
//            {
//                var topLevelCloseReasonNode = closeVoteTag.ParentElement.QuerySelector("#pane-main span.action-name");
//                int closeVoteTypeId;
//                if (topLevelCloseReasonNode != null)
//                {
//                    var topLevelCloseReason = topLevelCloseReasonNode.TextContent;
//                    switch (topLevelCloseReason)
//                    {
//                        case "duplicate of...":
//                            closeVoteTypeId = 1000;
//                            break;
//                        case "off-topic because...":
//                            continue;
//                        case "unclear what you're asking":
//                            closeVoteTypeId = 1001;
//                            break;
//                        case "too broad":
//                            closeVoteTypeId = 1002;
//                            break;
//                        case "primarily opinion-based":
//                            closeVoteTypeId = 1003;
//                            break;
//                        default:
//                            continue;
//                    }
//                }
//                else
//                {
//                    var offtopicLevelCloseReasonNode = closeVoteTag.ParentElement.ParentElement.QuerySelector("input[name=\"close-as-off-topic-reason\"]");
//                    if (offtopicLevelCloseReasonNode == null)
//                        continue;

//                    closeVoteTypeId = int.Parse(offtopicLevelCloseReasonNode.GetAttribute("value"));
//                }
//                if (!votes.ContainsKey(closeVoteTypeId))
//                    votes[closeVoteTypeId] = 0;

//                votes[closeVoteTypeId] += int.Parse(closeVoteTag.TextContent);
//            }

//            return votes;
//        }
//    }
//}
