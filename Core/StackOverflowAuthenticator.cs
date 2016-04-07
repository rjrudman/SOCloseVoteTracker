using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Core.RestRequests;
using RestSharp;

namespace Core
{
    public class StackOverflowAuthenticator
    {
        private const string BASE_URL = @"https://stackoverflow.com";

        private readonly string _username;
        private readonly string _password;

        private static DateTime _refreshCookieTime = DateTime.MinValue;
        private static readonly List<RestResponseCookie> AuthCookies = new List<RestResponseCookie>();
        private static readonly object CookieLocker = new object();

        private static bool CurrentAuthCookiesValid()
        {
            lock(CookieLocker)
                return _refreshCookieTime > DateTime.Now;
        }

        public StackOverflowAuthenticator(string username, string password)
        {
            _username = username;
            _password = password;
        }

        private IList<RestResponseCookie> GetAuthCookies()
        {
            lock (CookieLocker)
            {
                if (!CurrentAuthCookiesValid())
                {
                    var throttler = new RestRequestThrottler(BASE_URL, "users/login", Method.POST);
                    throttler.Client.FollowRedirects = false;

                    throttler.Request.AddParameter("email", _username);
                    throttler.Request.AddParameter("password", _password);

                    var response = throttler.Execute();
                    if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Found)
                        throw new Exception("Failed to authenticate. Status recieved: " + response.StatusCode);

                    AuthCookies.AddRange(response.Cookies);
                    _refreshCookieTime = AuthCookies.Min(ac => ac.Expires);
                }

                return AuthCookies.ToList();
            }
        }

        public void AuthenticateRequest(IRestRequest request)
        {
            foreach (var cookie in GetAuthCookies())
                request.AddCookie(cookie.Name, cookie.Value);
        }
        
    }
}
