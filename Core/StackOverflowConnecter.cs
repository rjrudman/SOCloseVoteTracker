﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Core.StackOverflowResults;
using Data.Entities;
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

        public IList<RecentCloseVote> GetRecentCloseVoteIds()
        {
            var restClient = new RestClient(SITE_URL);

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
                    QuestionId = id,
                    DateSeen = DateTime.Now,
                    NumVotes = votes,
                    VoteType = reason
                };
            }).ToList();
        }

        public Question GetQuestionInformation(int questionId)
        {
            var restClient = new RestClient(SITE_URL);

            var restRequest = new RestRequest($"questions/{questionId}", Method.GET);
            _authenticator.AuthenticateRequest(restRequest);
            
            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();

            var tags = parser.Result.QuerySelectorAll(".post-taglist .post-tag").Select(t => t.TextContent);
            var numFlags = int.Parse(parser.Result.QuerySelector(".existing-flag-count")?.TextContent ?? "0");
            var title = parser.Result.QuerySelector(".question-hyperlink").TextContent;
            var isClosed = parser.Result.QuerySelectorAll(".question-status").Any();

            return new Question
            {
                Closed = isClosed,
                VoteCount = numFlags,
                Title = title,
                Id = questionId,
                Tags = tags.Select(t => new Tag {TagName = t}).ToList()
            };
        }
    }
}
