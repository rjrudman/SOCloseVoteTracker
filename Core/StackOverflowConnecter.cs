using RestSharp;

namespace Core
{
    public class StackOverflowConnecter
    {
        private const string BaseUrl = @"https://stackoverflow.com";

        private readonly StackOverflowAuthenticator _authenticator = new StackOverflowAuthenticator();
        
        public void GetRecentlyClosed()
        {
            var restClient = new RestClient(BaseUrl);

            var restRequest = new RestRequest("tools", Method.GET);
            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", "recentlyClosed");

            _authenticator.AuthenticateRequest(restRequest);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            var response = restClient.Execute(restRequest);
        }
    }
}
