using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Text;
using System.Web;
using RestSharp;
using Utils;

namespace SOCloseVoteTracker
{
    class Program
    {
        private const string BaseURL = @"https://stackoverflow.com";
        static void Main(string[] args)
        {
            GetRecentlyClosed();
        }

        private static List<RestResponseCookie> AuthCookies = new List<RestResponseCookie>();

        private static bool RequiresLogin()
        {
            return !AuthCookies.Any() || AuthCookies.Any(ac => ac.Expired);
        }

        static void Login()
        {
            var restClient = new RestClient(BaseURL);
            var restRequest = new RestRequest("users/login", Method.POST);
            restRequest.AddParameter("email", Configuration.UserName);
            restRequest.AddParameter("password", Configuration.Password);
            restClient.FollowRedirects = false;
            var response = restClient.Execute(restRequest);
            AuthCookies.AddRange(response.Cookies);
        }
        
        static void GetRecentlyClosed()
        {
            if (RequiresLogin())
                Login();
            
            var restClient = new RestClient(BaseURL);
            var restRequest = new RestRequest("tools", Method.GET);
            restRequest.AddParameter("tab", "close");
            restRequest.AddParameter("daterange", "today");
            restRequest.AddParameter("mode", "recentlyClosed");

            foreach (var authCookie in AuthCookies)
                restRequest.AddCookie(authCookie.Name, authCookie.Value);

            restRequest.AddHeader("X-Requested-With", "XMLHttpRequest");

            var response = restClient.Execute(restRequest);
        }
    }
}
