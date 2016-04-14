using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;
using StackExchangeScraper.APIModels;
using Utils;

namespace StackExchangeScraper
{
    public static class StackExchangeAPI
    {
        private static DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string API_URL = @"https://api.stackexchange.com/2.2/";
        private const string QUESTION_FILTER_ID = "!m)ASytO_4PYB_0kLxW52mH3C9Mvtrk_Q.PoPC9pqVcH-xQGJPH6oSGjD";
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
        private static void AuthorizeRequest(IRestRequest attachToRequest)
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
        }

        private static DateTime GetDateTimeFromUnixLong(long dateNum)
        {
            return _unixEpoch.AddSeconds(dateNum);
        }

        public static IEnumerable<QuestionModel> GetQuestions(Dictionary<int, QuestionModel> questions)
        {
            var questionIdString = string.Join(";", questions.Select(q => q.Key));

            var client = new RestClient(API_URL);
            var request = new RestRequest($"questions/{questionIdString}");
            request.AddParameter("filter", QUESTION_FILTER_ID);
            request.AddParameter("site", "stackoverflow.com");
            AuthorizeRequest(request);

            var response = client.Execute(request);
            var responseObject = JsonConvert.DeserializeObject<BaseApiModel<QuestionApiModel>>(response.Content);
            var returnData = new List<QuestionModel>();
            foreach (var item in responseObject.Items)
            {
                var currentInfo = new QuestionModel
                {
                    Asked = GetDateTimeFromUnixLong(item.CreateDateInt),
                    Closed = item.ClosedDetails != null,
                    Deleted = false, //Not sure at the moment..
                    DeleteVotes = item.DeleteVotes,
                    ReopenVotes = item.ReopenVotes,
                    UndeleteVotes = 0,
                    Tags = item.Tags,
                    Id = item.QuestionId,
                    Title = item.Title,
                };

                if (item.ClosedDetails != null)
                {
                    if (item.ClosedDetails.OriginalQuestions != null && item.ClosedDetails.OriginalQuestions.Any())
                    {
                        var dupeTarget = item.ClosedDetails.OriginalQuestions.Select(oq => oq.QuestionId).FirstOrDefault();
                        currentInfo.DuplicateParentId = dupeTarget;
                        currentInfo.Dependencies.Add(dupeTarget);
                    }
                }
                else
                {
                    if (questions[currentInfo.Id] == null || questions[currentInfo.Id].CloseVotes.Sum(cv => cv.Value) != item.CloseVotes)
                    {
                        //Now we need to look at the close votes for this question
                        //Queue it up
                    }
                }
                returnData.Add(currentInfo);
            }
            
            return returnData;
        }
    }
}
