using System.Threading;
using Newtonsoft.Json;
using Utils;
using WebSocketSharp;

namespace Core.Scrapers.Sockets
{
    public abstract class BaseStackExchangeSocket<TMessageType>
        where TMessageType : class
    {
        protected const string StackExchangeSocketsUrl = "ws://qa.sockets.stackexchange.com";

        private WebSocket _socket;
        private bool _disposed;

        public WebSocketState SocketState => _socket?.ReadyState ?? WebSocketState.Closed;

        protected BaseStackExchangeSocket()
        {
            InitialiseSocket();
        }

        private void InitialiseSocket()
        {
            _socket = new WebSocket(StackExchangeSocketsUrl);
            if (!string.IsNullOrWhiteSpace(GlobalConfiguration.ProxyUrl))
                _socket.SetProxy(GlobalConfiguration.ProxyUrl, GlobalConfiguration.ProxyUsername, GlobalConfiguration.ProxyPassword);

            _socket.OnOpen += (o, oo) =>
                _socket.Send(ActionRequest);

            _socket.OnError += HandleException;
            _socket.OnMessage += (obj, args) =>
            {
                new Thread(() =>
                {
                    var wrapperObject = JsonConvert.DeserializeObject<ResponseObjectWrapper>(args.Data);
                    var actualData = JsonConvert.DeserializeObject<TMessageType>(wrapperObject.Data);
                    OnMessage(actualData);
                }).Start();
            };

            _socket.OnClose += (o, oo) =>
            {
                if (_disposed)
                    return;

                InitialiseSocket();
            };
        }


        public void Dispose()
        {
            _disposed = true;
            Close();
        }

        public void Connect()
        {
            if (SocketState == WebSocketState.Connecting || SocketState == WebSocketState.Closed)
                _socket?.Connect();
        }

        public void Close()
        {
            if (SocketState == WebSocketState.Open)
                _socket?.Close();
        }

        protected virtual void HandleException(object obj, ErrorEventArgs args)
        {
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ResponseObjectWrapper
        {
            [JsonProperty("action")]
            // ReSharper disable once UnusedMember.Local
            public string Action { get; set; }

            [JsonProperty("data")]
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Data { get; set; }
        }

        protected abstract string ActionRequest { get; }
        protected abstract void OnMessage(TMessageType obj);
    }
}
