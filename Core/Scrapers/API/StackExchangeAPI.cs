using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Core.Scrapers.API.APIModels;
using Core.Scrapers.Authentication;
using Core.Scrapers.Models;
using Core.Scrapers.Utils;
using Core.Workers;
using Data;
using Data.Entities;
using Newtonsoft.Json;
using RestSharp;
using Utils;

namespace Core.Scrapers.API
{
    public static class StackExchangeAPI
    {
        private static DateTime _unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string API_URL = @"https://api.stackexchange.com/2.2/";

        private const string QUESTION_FILTER_ID = "!m)ASytO_4PYB_0kLxW52mH3C9Mvtrk_Q.PoPC9pqVcH-xQGJPH6oSGjD";
        private const string CLOSE_VOTE_FILTER_ID = "!4-g4u*Y888my2lYD(";

        private static readonly StackOverflowAuthenticator Authenticator = new StackOverflowAuthenticator(
            GlobalConfiguration.UserName, 
            GlobalConfiguration.Password
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

        private const int MAX_CONCURRENT_REQUESTS = 25;
        private const int TIMESPAN_PER_REQUEST = 1; //One second
        private static readonly TimeSpanSemaphore GlobalThrottle = new TimeSpanSemaphore(MAX_CONCURRENT_REQUESTS, TimeSpan.FromSeconds(TIMESPAN_PER_REQUEST));

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

        public static IEnumerable<QuestionModel> GetQuestions(IList<int> questionIds)
        {
            var questionIdString = string.Join(";", questionIds);

            var client = new RestClient(API_URL);
            var request = new RestRequest($"questions/{questionIdString}");
            request.AddParameter("filter", QUESTION_FILTER_ID);
            request.AddParameter("site", "stackoverflow.com");
            request.AddParameter("page", 1);
            request.AddParameter("pagesize", 100);

            var response = client.AuthenticateAndThrottle<QuestionApiModel>(request);
            
            var returnData = new List<QuestionModel>();

            using (var context = new ReadWriteDataContext())
            {
                var questionMapping = context.Questions.Where(q => questionIds.Contains(q.Id)).ToDictionary(q => q.Id, q => q);

                foreach (var item in response.Items)
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
                        }
                    }
                    else
                    {
                        if (!questionMapping.ContainsKey(currentInfo.Id) || questionMapping[currentInfo.Id].CloseVotes.Count != item.CloseVotes)
                            Pollers.QueueCloseVoteQuery(currentInfo.Id);
                    }
                    returnData.Add(currentInfo);
                }
            }

            return returnData;
        }

        //Move this to the database..?
        private static readonly Dictionary<string, int> VoteTypeTitleMapping = new Dictionary<string, int>
        {
            {"Migration", 2},
            {"Questions about <b>general computing hardware and software</b> are off-topic for Stack Overflow unless they directly involve tools used primarily for programming. You may be able to get help on <a href=\"http://superuser.com/about\">Super User</a>.", 4},
            {"Questions on <b>professional server- or networking-related infrastructure administration</b> are off-topic for Stack Overflow unless they directly involve programming or programming tools. You may be able to get help on <a href=\"http://serverfault.com/about\">Server Fault</a>.", 7},
            {"Questions asking us to <b>recommend or find a book, tool, software library, tutorial or other off-site resource</b> are off-topic for Stack Overflow as they tend to attract opinionated answers and spam. Instead, <a href=\"http://meta.stackoverflow.com/questions/254393\">describe the problem</a> and what has been done so far to solve it.", 16},
            {"Questions seeking debugging help (\"<b>why isn't this code working?</b>\") must include the desired behavior, a <i>specific problem or error</i> and <i>the shortest code necessary</i> to reproduce it <b>in the question itself</b>. Questions without <b>a clear problem statement</b> are not useful to other readers. See: <a href=\"http://stackoverflow.com/help/mcve\">How to create a Minimal, Complete, and Verifiable example</a>.", 13},
            {"This question was caused by <b>a problem that can no longer be reproduced</b> or <b>a simple typographical error</b>. While similar questions may be on-topic here, this one was resolved in a manner unlikely to help future readers. This can often be avoided by identifying and closely inspecting <a href=\"http://stackoverflow.com/help/mcve\">the shortest program necessary to reproduce the problem</a> before posting.", 11},

            {"Duplicate", 1000 },
            {"Please clarify your specific problem or add additional details to highlight exactly what you need. As it's currently written, it’s hard to tell exactly what you're asking.", 1001},
            {"There are either too many possible answers, or good answers would be too long for this format. Please add details to narrow the answer set or to isolate an issue that can be answered in a few paragraphs.", 1002},
            {"Many good questions generate some degree of opinion based on expert experience, but answers to this question will tend to be almost entirely based on opinions, rather than facts, references, or specific expertise.", 1003},
        };

        public static IDictionary<int, int> GetQuestionVotes(int questionId)
        {
            var client = new RestClient(API_URL);
            var request = new RestRequest($"questions/{questionId}/close/options");
            request.AddParameter("filter", CLOSE_VOTE_FILTER_ID);
            request.AddParameter("site", "stackoverflow.com");
            
            var response = client.AuthenticateAndThrottle<CloseVoteApiModel>(request);

            var flattenedCloseVotes = FlattenCloseVotes(response.Items);
            var voteTypesWithCount = flattenedCloseVotes.Where(v => v.Count > 0);
            var voteResults = new Dictionary<int, int>();

            foreach (var voteType in voteTypesWithCount)
            {
                int matchingVoteReason;
                if (VoteTypeTitleMapping.ContainsKey(voteType.Description))
                    matchingVoteReason = VoteTypeTitleMapping[voteType.Description];
                else if (VoteTypeTitleMapping.ContainsKey(voteType.Title))
                    matchingVoteReason = VoteTypeTitleMapping[voteType.Title];
                else
                    matchingVoteReason = 3; //'Other'

                if (!voteResults.ContainsKey(matchingVoteReason))
                    voteResults[matchingVoteReason] = 1;
                else
                    voteResults[matchingVoteReason]++;
            }
            return voteResults;
        }

        private static IEnumerable<CloseVoteApiModel> FlattenCloseVotes(IEnumerable<CloseVoteApiModel> votes)
        {
            while (true)
            {
                var closeVoteApiModels = votes as IList<CloseVoteApiModel> ?? votes.ToList();
                if (closeVoteApiModels.All(v => v.SubOptions == null))
                    return closeVoteApiModels;

                votes = closeVoteApiModels.Where(v => v.SubOptions == null).Union(closeVoteApiModels.Where(v => v.SubOptions != null).SelectMany(v => v.SubOptions));
            }
        }

        private static BaseApiModel<TModelType> AuthenticateAndThrottle<TModelType>(this IRestClient client, IRestRequest request)
        {
            BaseApiModel<TModelType> responseObject = null;
            GlobalThrottle.Run(() =>
            {
                AuthorizeRequest(request);
                ThrottleRequest();
            
                var response = client.Execute(request);
                responseObject = JsonConvert.DeserializeObject<BaseApiModel<TModelType>>(response.Content);
                AssignNewThrottle(responseObject);
            }, new CancellationToken());
            return responseObject;
        }


        private static readonly object ThrottleLocker = new object();
        private static DateTime _nextAllowedRequestTime = DateTime.MinValue;

        private static void ThrottleRequest()
        {
            lock (ThrottleLocker)
            {
                if (_nextAllowedRequestTime > DateTime.Now)
                    Thread.Sleep((int)Math.Ceiling((_nextAllowedRequestTime - DateTime.Now).TotalMilliseconds));
            }
        }

        private static void AssignNewThrottle<TModelType>(BaseApiModel<TModelType> response)
        {
            DateTime? nextAllowedTime = null;
            if (response.QuotaRemaining == 0)
                nextAllowedTime = DateTime.UtcNow.Date.AddDays(1);
            else if (response.BackOff.HasValue)
                nextAllowedTime = DateTime.Now.AddSeconds(response.BackOff.Value + 2);

            if (nextAllowedTime.HasValue)
            {
                lock (ThrottleLocker)
                    _nextAllowedRequestTime = nextAllowedTime.Value;
            }
        }
    }
}
