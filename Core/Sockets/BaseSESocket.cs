using System.Threading;
using Newtonsoft.Json;
using Utils;
using WebSocketSharp;

namespace Core.Sockets
{
    public abstract class BaseSESocket<TMessageType>
        where TMessageType : class
    {
        protected const string SESocketsUrl = "ws://qa.sockets.stackexchange.com";

        private WebSocket socket;
        private bool disposed;

        public WebSocketState SocketState => socket?.ReadyState ?? WebSocketState.Closed;

        protected BaseSESocket()
        {
            InitialiseSocket();
        }

        private void InitialiseSocket()
        {
            socket = new WebSocket(SESocketsUrl);
            if (!string.IsNullOrWhiteSpace(Configuration.ProxyUrl))
                socket.SetProxy(Configuration.ProxyUrl, Configuration.ProxyUsername, Configuration.ProxyPassword);

            socket.OnOpen += (o, oo) =>
                socket.Send(ActionRequest);

            socket.OnError += HandleException;
            socket.OnMessage += (obj, args) =>
            {
                new Thread(() =>
                {
                    var wrapperObject = JsonConvert.DeserializeObject<ResponseObjectWrapper>(args.Data);
                    var actualData = JsonConvert.DeserializeObject<TMessageType>(wrapperObject.Data);
                    OnMessage(actualData);
                }).Start();
            };

            socket.OnClose += (o, oo) =>
            {
                if (disposed)
                    return;

                InitialiseSocket();
            };
        }


        public void Dispose()
        {
            disposed = true;
            Close();
        }

        public void Connect()
        {
            if (SocketState == WebSocketState.Connecting || SocketState == WebSocketState.Closed)
                socket?.Connect();
        }

        public void Close()
        {
            if (SocketState == WebSocketState.Open)
                socket?.Close();
        }

        protected virtual void HandleException(object obj, ErrorEventArgs args)
        {
        }

        private class ResponseObjectWrapper
        {
            [JsonProperty("action")]
            public string Action { get; set; }

            [JsonProperty("data")]
            public string Data { get; set; }
        }

        protected abstract string ActionRequest { get; }
        protected abstract void OnMessage(TMessageType obj);
    }
}
