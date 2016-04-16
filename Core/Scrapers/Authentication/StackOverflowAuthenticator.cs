using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Core.Scrapers.Utils;
using RestSharp;

namespace Core.Scrapers.Authentication
{
    public class StackOverflowAuthenticator
    {
        private const string BASE_URL = @"https://stackoverflow.com";

        private readonly string _username;
        private readonly string _password;
        private readonly string[] _alsoAuthenticateSisterSites;

        private static DateTime _refreshCookieTime = DateTime.MinValue;
        private static readonly List<RestResponseCookie> AuthCookies = new List<RestResponseCookie>();
        private static readonly object CookieLocker = new object();

        private static bool CurrentAuthCookiesValid()
        {
            lock(CookieLocker)
                return _refreshCookieTime > DateTime.Now;
        }

        public StackOverflowAuthenticator(string username, string password, params string[] alsoAuthenticateSisterSites)
        {
            _username = username;
            _password = password;
            _alsoAuthenticateSisterSites = alsoAuthenticateSisterSites;
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

                    AuthenticateSisterSites();
                }

                return AuthCookies.ToList();
            }
        }

        public void AuthenticateRequest(IRestRequest request)
        {
            foreach (var cookie in GetAuthCookies())
                request.AddCookie(cookie.Name, cookie.Value);
        }

        private void AuthenticateSisterSites()
        {
            if (!_alsoAuthenticateSisterSites.Any())
                return;

            var globalAuthClient = new RestClient(@"http://stackoverflow.com");
            var globalAuthRequest = new RestRequest(@"/users/login/universal/request", Method.POST);
            globalAuthRequest.AddHeader("Referer", "http://stackoverflow.com/");
            AuthenticateRequest(globalAuthRequest);

            var response = globalAuthClient.Execute<List<SisterSiteAuthTokens>>(globalAuthRequest);
            var sisterSiteTokenInfos = response.Data.ToDictionary(d => d.Host, d => d);

            foreach (var sisterSite in _alsoAuthenticateSisterSites)
            {
                if (!sisterSiteTokenInfos.ContainsKey(sisterSite))
                    throw new Exception($"Unable to globally authenticate sister site '{sisterSite}'");

                var sisterSiteTokenInfo = sisterSiteTokenInfos[sisterSite];

                var sisterSiteClient = new RestClient($"http://{sisterSite}");
                var sisterSiteRequest = new RestRequest("/users/login/universal.gif", Method.GET);
                sisterSiteRequest.AddHeader("Referer", "http://stackoverflow.com/");
                sisterSiteRequest.AddParameter("authToken", sisterSiteTokenInfo.Token);
                sisterSiteRequest.AddParameter("nonce", sisterSiteTokenInfo.Nonce);

                var sisterSiteResponse = sisterSiteClient.Execute(sisterSiteRequest);

                AuthCookies.AddRange(sisterSiteResponse.Cookies);
                _refreshCookieTime = AuthCookies.Min(ac => ac.Expires);
            }
        }

        private class SisterSiteAuthTokens
        {
            public string Token { get; set; }
            public string Nonce { get; set; }
            public string Host { get; set; }
        }
    }
}
