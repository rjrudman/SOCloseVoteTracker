using System;
using System.Collections.Generic;
using AngleSharp.Html;
using Core.Scrapers.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Utils;
using WebSocketSharp;

namespace Core.Scrapers.Chat
{
    public static class LiteChatScraper
    {
        public static void JoinAndWatchRoom(long roomId, Action<Dictionary<string, JObject>> onMessage)
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
            var eventsResponseSerialized = JsonConvert.DeserializeAnonymousType(eventsResponse.Content, new { time = 0 });

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
                var parsedMessage = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(messageArgs.Data);
                onMessage(parsedMessage);
            };
            socket.OnClose += (sender, args) =>
            {
                JoinAndWatchRoom(roomId, onMessage);
            };
            socket.Connect();
        }
    }
}
