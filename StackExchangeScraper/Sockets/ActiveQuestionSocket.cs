using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Core.Sockets;
using Newtonsoft.Json;
using WebSocketSharp;

namespace StackExchangeScraper.Sockets
{
    public class ActiveQuestion
    {
        [JsonProperty("siteBaseHostAddress")]
        public string SiteBaseHostAddress { get; set; }
        [JsonProperty("id")]
        public uint ID { get; set; }
        [JsonProperty("titleEncodedFancy")]
        public string TitleEncodedFancy { get; set; }
        [JsonProperty("bodySummary")]
        public string BodySummary { get; set; }
        [JsonProperty("tags")]
        public List<string> Tags { get; set; }
        [JsonProperty("lastActivityDate")]
        public int LastActivityDate { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("ownerUrl")]
        public string OwnerUrl { get; set; }
        [JsonProperty("ownerDisplayName")]
        public string OwnerDisplayName { get; set; }
        [JsonProperty("apiSiteParameter")]
        public string ApiSiteParameter { get; set; }
    }

    public class ActiveQuestionsPoller : BaseStackExchangeSocket<ActiveQuestion>
    {
        public delegate void OnNewQuestionEventHandler(ActiveQuestion q);
        public delegate void OnExceptionEventHandler(Exception ex);

        public event OnNewQuestionEventHandler OnNewQuestion;
        public event OnExceptionEventHandler OnException;

        private readonly ExchangeSite site;
        public ActiveQuestionsPoller(ExchangeSite site = ExchangeSite.AllSites)
        {
            this.site = site;
        }

        protected override string ActionRequest => $"{(int)site}-questions-active";
        protected override void OnMessage(ActiveQuestion obj)
        {
            OnNewQuestion?.Invoke(obj);
        }

        protected override void HandleException(object obj, ErrorEventArgs args)
        {
            OnException?.Invoke(args?.Exception);
        }

        //Keep a reference to them so they're not cleaned up!
        private static readonly ConcurrentBag<ActiveQuestionsPoller> Pollers = new ConcurrentBag<ActiveQuestionsPoller>();

        //Allows you to easily register a callback without having to worry about keeping a reference to the poller
        public static void Register(OnNewQuestionEventHandler onNewQuestion, ExchangeSite site = ExchangeSite.AllSites)
        {
            Register(onNewQuestion, ex => { }, site);
        }
        public static void Register(OnNewQuestionEventHandler onNewQuestion, OnExceptionEventHandler onException, ExchangeSite site = ExchangeSite.AllSites)
        {
            var poller = new ActiveQuestionsPoller(site);
            poller.OnNewQuestion += onNewQuestion;
            poller.OnException += onException;
            poller.Connect();
            Pollers.Add(poller);
        }
    }
}
