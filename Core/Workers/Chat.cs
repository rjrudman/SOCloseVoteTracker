using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Html;
using Dapper;
using Data;
using Hangfire;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using WebSocketSharp;
using GlobalConfiguration = Utils.GlobalConfiguration;

namespace Core.Workers
{
    public class Chat
    {
        const string UPSERT_CVPLS_SQL = @"INSERT INTO CVPlsRequests(UserId, QuestionId, FullMessage, CreatedAt) VALUES (@UserId, @QuestionId, @FullMessage, GETUTCDATE())";
        private static readonly Regex QuestionIdRegex = new Regex("\\/(q(uestions)?|p(osts)?)\\/(?<questionID>\\d+)\\/.*");
        public static void JoinAndWatchRoom(long roomId)
        {
            
            var authenticator = new StackOverflowAuthenticator(GlobalConfiguration.UserName, GlobalConfiguration.Password);
            var restClient = new RestClient("http://chat.stackoverflow.com/");
            var roomRequest = new RestRequest($"rooms/{roomId}", Method.GET);
            authenticator.AuthenticateRequest(roomRequest);

            //Join the room
            var roomRequestResponse = restClient.Execute(roomRequest);
            var parser = new HtmlParser(roomRequestResponse.Content);
            parser.Parse();

            var keyElement = parser.Result.QuerySelector("#fkey");
            var fkey = keyElement.GetAttribute("value");

            var eventsRequest = new RestRequest($"chats/{roomId}/events", Method.POST);
            authenticator.AuthenticateRequest(eventsRequest);
            eventsRequest.AddParameter("since", 0);
            eventsRequest.AddParameter("mode", "Messages");
            eventsRequest.AddParameter("msgCount", 100);
            eventsRequest.AddParameter("fkey", fkey);

            var eventsResponse = restClient.Execute(eventsRequest);
            var eventsResponseSerialized = JsonConvert.DeserializeAnonymousType(eventsResponse.Content, new {time = 0});

            
            var wsAuthRequest = new RestRequest("ws-auth", Method.POST);
            authenticator.AuthenticateRequest(wsAuthRequest);
            wsAuthRequest.AddParameter("roomid", roomId);
            wsAuthRequest.AddParameter("fkey", fkey);

            var authResponse = restClient.Execute(wsAuthRequest);
            var authResponseObj = JsonConvert.DeserializeAnonymousType(authResponse.Content, new { url = string.Empty });
            var websocketURL = $"{authResponseObj.url}?l={eventsResponseSerialized.time}";
            
            var socket = new WebSocket(websocketURL) { Origin = "http://chat.stackoverflow.com" };
            if (!string.IsNullOrWhiteSpace(GlobalConfiguration.ProxyUrl))
                socket.SetProxy(GlobalConfiguration.ProxyUrl, GlobalConfiguration.UserName, GlobalConfiguration.Password);

            socket.OnMessage += (messageSender, messageArgs) =>
            {
                var messages = new[] {new { userId = 0, content = string.Empty}}.ToList();
                messages.Clear();
                
                try
                {
                    var obj = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(messageArgs.Data);
                    var children = obj[$"r{roomId}"].Children();
                    foreach (var child in children)
                    {
                        if (child.Path == "e")
                        {
                            var subChildren = child.Children();
                            foreach (var subChild in subChildren)
                            {
                                foreach (var @event in subChild)
                                {
                                    var userId = @event.Value<int>("user_id");
                                    var message = @event.Value<string>("content");
                                    messages.Add(new {userId = userId, content = message});
                                }
                            }
                        }
                    }

                } catch(Exception) { }
                foreach (var message in messages)
                    ParseContent(message.userId, message.content);
            };
            socket.OnClose += (sender, args) => { JoinAndWatchRoom(roomId); };
            socket.Connect();
        }

        private static void ParseContent(int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            if (!content.Contains(@"<a href=""//stackoverflow.com/questions/tagged/cv-pls"">"))
                return;

            var matches = QuestionIdRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var questionIDstr = match.Groups["questionID"].Value;
                int questionId;

                if (int.TryParse(questionIDstr, out questionId))
                    BackgroundJob.Enqueue(() => QueryQuestionAndLogRequest(userId, questionId, content));
            }
        }

        public static void QueryQuestionAndLogRequest(int userId, int questionId, string fullMessage)
        {
            Pollers.QueryQuestion(questionId, DateTime.Now, false);

            using (var connection = DataContext.PlainConnection())
                connection.Execute(UPSERT_CVPLS_SQL, new { UserId = userId, QuestionId = questionId, FullMessage = fullMessage });

            //Check on the question again in an hour
            Pollers.QueueQuestionQuery(questionId, TimeSpan.FromHours(1));
        }
    }
}
