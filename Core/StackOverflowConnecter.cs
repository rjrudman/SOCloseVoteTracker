using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Core.Models;
using Core.StackOverflowResults;
using RestSharp;
using Utils;

namespace Core
{
    public class StackOverflowConnecter
    {
        private const string API_URL = @"https://api.stackexchange.com/2.2";
        private const string SITE_URL = @"https://stackoverflow.com";

        private readonly StackOverflowAuthenticator _authenticator = new StackOverflowAuthenticator(Configuration.UserName, Configuration.Password);
        private readonly Regex _questionIdRegex = new Regex("\\/questions\\/(?<questionID>\\d+)\\/.*");

        private IList<int> GetCloseVoteQueue(string mode)
        {
            var restClient = new RestClient(SITE_URL);

            var restRequest = new RestRequest("tools", Method.GET);
            _authenticator.AuthenticateRequest(restRequest);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", mode);

            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var rows = parser.Result.QuerySelectorAll("table tr");
            return rows.Select(r =>
            {
                var link = r.QuerySelector("td a");
                var url = link.GetAttribute("href");

                var match = _questionIdRegex.Match(url);
                var id = int.Parse(match.Groups["questionID"].Value);

                return id;
            }).ToList();
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
            var restClient = new RestClient(SITE_URL);

            var restRequest = new RestRequest($"questions/{questionId}", Method.GET);
            _authenticator.AuthenticateRequest(restRequest);
            
            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var tags = parser.Result.QuerySelectorAll(".post-taglist .post-tag").Select(t => t.TextContent);
            var title = parser.Result.QuerySelector(".question-hyperlink").TextContent;
            var isClosed = parser.Result.QuerySelectorAll(".question-status").Any();

            var votes = isClosed 
                ? new Dictionary<int, int>() : 
                GetCloseVotes(questionId);
            
            return new QuestionModel
            {
                Closed = isClosed,
                Title = title,
                Id = questionId,
                Tags = tags.ToList(),
                CloseVotes = votes
            };
        }

        //TODO: Tidy this logic up a bit (a central area mapping top-level close reason to an ID would be nice).
        private Dictionary<int, int> GetCloseVotes(int questionId)
        {
            var restClient = new RestClient(SITE_URL);

            var restRequest = new RestRequest($"flags/questions/{questionId}/close/popup", Method.GET);
            _authenticator.AuthenticateRequest(restRequest);

            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var closeVoteTags = parser.Result.QuerySelectorAll(".bounty-indicator-tab[title=\"number of votes already cast\"]");

            var votes = new Dictionary<int, int>();

            foreach (var closeVoteTag in closeVoteTags)
            {
                var topLevelCloseReasonNode = closeVoteTag.ParentElement.QuerySelector("input[name=\"close-reason\"]");
                int closeVoteTypeId;
                if (topLevelCloseReasonNode != null)
                {
                    var topLevelCloseReason = topLevelCloseReasonNode.GetAttribute("value");
                    switch (topLevelCloseReason)
                    {
                        case "Duplicate":
                            closeVoteTypeId = 1000;
                            break;
                        case "OffTopic":
                            continue;
                        case "Unclear":
                            closeVoteTypeId = 1001;
                            break;
                        case "TooBroad":
                            closeVoteTypeId = 1002;
                            break;
                        case "OpinionBased":
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
