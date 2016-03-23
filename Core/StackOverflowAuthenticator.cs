using System;
using System.Collections.Generic;
using System.Linq;
using RestSharp;
using Utils;

namespace Core
{
    class StackOverflowAuthenticator
    {
        private const string BaseUrl = @"https://stackoverflow.com";

        private DateTime _refreshCookieTime = DateTime.MinValue;
        private readonly List<RestResponseCookie> _authCookies = new List<RestResponseCookie>();

        private bool CurrentAuthCookiesValid() =>_refreshCookieTime > DateTime.Now;

        private IEnumerable<RestResponseCookie> GetAuthCookies()
        {
            if (!CurrentAuthCookiesValid())
            {
                var restClient = new RestClient(BaseUrl);
                var restRequest = new RestRequest("users/login", Method.POST);
                restClient.FollowRedirects = false;

                restRequest.AddParameter("email", Configuration.UserName);
                restRequest.AddParameter("password", Configuration.Password);

                var response = restClient.Execute(restRequest);

                _authCookies.AddRange(response.Cookies);
                _refreshCookieTime = _authCookies.Min(ac => ac.Expires);

            }
            return _authCookies;
        }

        public void AuthenticateRequest(IRestRequest request)
        {
            foreach (var cookie in GetAuthCookies())
                request.AddCookie(cookie.Name, cookie.Value);
        }
        
    }
}
