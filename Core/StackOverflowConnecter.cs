using System.Collections;
using System.Collections.Generic;
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
        
        public IList<Question> GetRecentlyClosed()
        {
            var restClient = new RestClient(BaseUrl);

            var restRequest = new RestRequest("tools", Method.GET);
            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", "recentlyClosed");

            _authenticator.AuthenticateRequest(restRequest);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            var response = restClient.Execute(restRequest);
            var parser = new HtmlParser(response.Content);
            parser.Parse();
            var example = parser.Result.QuerySelectorAll("");
            return null;
        }
    }
}
