using System.Collections.Generic;
using System.Linq;
using RestSharp;
using StackExchangeScraper.RestRequests;
using Utils;

namespace StackExchangeScraper
{
    public static class StackExchangeAPI
    {
        private const string API_URL = @"https://api.stackexchange.com/2.2/";
        private const string QuestionFilterID = "!))xtOtk";
        private static readonly StackOverflowAuthenticator Authenticator = new StackOverflowAuthenticator(
            GlobalConfiguration.UserName, 
            GlobalConfiguration.Password,
            "stackexchange.com"
        );

        ////Remove this for production - we need to get it at startup (it will post back)
        //private const string access_token = @"uqT68yjR*oKzBNCiEvvBHw))";

        private static void AttachCredentials(RestRequest attachToRequest)
        {
            var alreadyHaveToken = false;
            if (!alreadyHaveToken)
            {
                var restClient = new RestClient(@"https://stackexchange.com");
                var restRequest = new RestRequest(@"oauth/dialog", Method.GET);
                restRequest.AddParameter("client_id", 6905);
                restRequest.AddParameter("redirect_uri", "soclosevotetrackerui.azurewebsites.net/authToken");
                restClient.FollowRedirects = false;
                Authenticator.AuthenticateRequest(restRequest);

                var response = restClient.Execute(restRequest);

            }
        }

        public static QuestionModel GetQuestions(IEnumerable<int> questionIds)
        {
            var questionIdString = string.Join(";", questionIds);

            var client = new RestClient(API_URL);
            var request = new RestRequest($"questions/{questionIdString}");
            AttachCredentials(request);

            //var rc = new RestRequestThrottler(API_URL, $"questions/{questionId}", Method.GET, _authenticator);
            //rc.Execute()

            return null;
        }
    }
}
