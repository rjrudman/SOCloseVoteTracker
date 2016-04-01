using System;
using System.Collections.Generic;
using System.Linq;
using Core.RestRequests;
using RestSharp;

namespace Core
{
    public class StackOverflowAuthenticator
    {
        private const string BASE_URL = @"https://stackoverflow.com";

        private readonly string _username;
        private readonly string _password;

        private DateTime _refreshCookieTime = DateTime.MinValue;
        private readonly List<RestResponseCookie> _authCookies = new List<RestResponseCookie>();

        private bool CurrentAuthCookiesValid() => _refreshCookieTime > DateTime.Now;

        public StackOverflowAuthenticator(string username, string password)
        {
            _username = username;
            _password = password;
        }

        private IEnumerable<RestResponseCookie> GetAuthCookies()
        {
            if (!CurrentAuthCookiesValid())
            {
                var throttler = new RestRequestThrottler(BASE_URL, "users/login", Method.POST);
                throttler.Client.FollowRedirects = false;

                throttler.Request.AddParameter("email", _username);
                throttler.Request.AddParameter("password", _password);

                var response = throttler.Execute();

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
