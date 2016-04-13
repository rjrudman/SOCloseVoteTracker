using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RestSharp;
using StackExchangeScraper.RestRequests;
using Utils;

namespace StackExchangeScraper
{
    public static class StackExchangeAPI
    {
        private static DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string API_URL = @"https://api.stackexchange.com/2.2/";
        private const string QuestionFilterID = "!))xtOtk";
        private static readonly StackOverflowAuthenticator Authenticator = new StackOverflowAuthenticator(
            GlobalConfiguration.UserName, 
            GlobalConfiguration.Password,
            "stackexchange.com"
        );

        //This is *NOT* a secret - fine to have it here.
        private const string KEY = @"C09d5MdMOtrvL9LuXjQlKg((";

        private static string _accessToken = string.Empty;
        private static readonly object AccessTokenLocker = new object();
        private static DateTime _accessTokenExpireTime = DateTime.MinValue;
        private static bool CurrentAccessTokenValid()
        {
            lock (AccessTokenLocker)
                return _accessTokenExpireTime > DateTime.Now;
        }

        private static readonly Regex AccessTokenRegex = new Regex(@"access_token=(?<accessToken>.+)&expires=(?<expires>\d+)");
        private static void ConfigureRequest(RestRequest attachToRequest)
        {
            lock (AccessTokenLocker)
            {
                if (!CurrentAccessTokenValid())
                {
                    //We don't actually redirect. We can read the access token from the redirect URL.
                    //This is useful when running in a console app/unable to expose an endpoint while in dev.
                    const string redirectURL = "http://soclosevotetrackerui.azurewebsites.net/authToken";
                    var restClient = new RestClient(@"https://stackexchange.com");
                    var restRequest = new RestRequest(@"oauth/dialog", Method.GET);
                    restRequest.AddParameter("client_id", 6905);
                    restRequest.AddParameter("redirect_uri", redirectURL);
                    restClient.FollowRedirects = false;
                    Authenticator.AuthenticateRequest(restRequest);

                    var response = restClient.Execute(restRequest);
                    var locationHeader = response.Headers.FirstOrDefault(h => h.Value.ToString().StartsWith(redirectURL));
                    if (locationHeader != null)
                    {
                        var regMatch = AccessTokenRegex.Match(locationHeader.Value.ToString());
                        if (regMatch.Success)
                        {
                            var accessToken = regMatch.Groups["accessToken"].Value;
                            var expiresNum = long.Parse(regMatch.Groups["expires"].Value);
                            var expiresDate = DateTime.Now.AddSeconds(expiresNum);
                            _accessTokenExpireTime = expiresDate.AddHours(-1);
                            _accessToken = accessToken;
                        }
                        else
                        {
                            throw new Exception("Could not parse access token from return URL");
                        }
                    }
                    else
                    {
                        throw new Exception("Could not find redirect header from login.");
                    }
                }
            }
            attachToRequest.AddParameter("key", KEY);
            attachToRequest.AddParameter("access_token", _accessToken);
            attachToRequest.AddParameter("filter", QuestionFilterID);
            attachToRequest.AddParameter("site", "stackoverflow.com");
        }

        private static DateTime GetDateTimeFromUnixLong(long dateNum)
        {
            return _unixEpoch.AddSeconds(dateNum);
        }

        public static QuestionModel GetQuestions(IEnumerable<int> questionIds)
        {
            var questionIdString = string.Join(";", questionIds);

            var client = new RestClient(API_URL);

            //Make the filter..
            var firstRequest = new RestRequest(@"/2.2/filters/create", Method.POST);
            firstRequest.AddParameter("include", "close_vote_count;closed_date;delete_vote_count;reopen_vote_count");
            firstRequest.AddParameter("exclude", "exclude=accepted_answer_id;answer_count;bounty_amount;bounty_closes_date;community_owned_date;is_answered;last_activity_date;last_edit_date;locked_date;migrated_from;owner;protected_date;score;view_count");
            firstRequest.AddParameter("unsafe", false);
            firstRequest.AddParameter("key", KEY);
            firstRequest.AddParameter("access_token", _accessToken);

            var f = client.Execute(firstRequest);

            var request = new RestRequest($"questions/{questionIdString}");
            ConfigureRequest(request);

            var response = client.Execute(request);

            return null;
        }
    }
}
