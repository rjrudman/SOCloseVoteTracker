using System;
using System.Threading;
using RestSharp;

namespace Core.RestRequests
{
    public class RestRequestThrottler
    {
        //SO deems 30 requests / sec as abusive, and caps the API at that. Let's stay a bit under (25 / sec)
        private const int MAX_CONCURRENT_REQUESTS = 25;
        private const int TIMESPAN_PER_REQUEST = 1; //One second

        private readonly StackOverflowAuthenticator _authenticator;
        public readonly RestClient Client;
        public readonly RestRequest Request;
        private static readonly TimeSpanSemaphore _semaphore = new TimeSpanSemaphore(MAX_CONCURRENT_REQUESTS, TimeSpan.FromSeconds(TIMESPAN_PER_REQUEST));

        public RestRequestThrottler(string baseURL, string resource, Method method, StackOverflowAuthenticator authenticator = null)
        {
            _authenticator = authenticator;

            Client = new RestClient(baseURL);
            Request = new RestRequest(resource, method);
        }

        public IRestResponse Execute()
        {
            _authenticator?.AuthenticateRequest(Request);

            IRestResponse response = null;
            _semaphore.Run(() =>
            {
                response = Client.Execute(Request);
            }, new CancellationToken());

            return response;
        }
    }
}
