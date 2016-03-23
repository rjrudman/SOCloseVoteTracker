using System.Net;
using System.Text;
using Utils;

namespace SOCloseVoteTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            GetRecentlyClosed();
        }

        static void GetRecentlyClosed()
        {
            var endPoint = @"http://stackoverflow.com/tools?tab=close&daterange=today&mode=recentlyClosed";
            var client = new WebClient();

            client.Headers.Add(HttpRequestHeader.Cookie, $"acct={Configuration.AuthKey}");
            client.Headers.Add("X-Requested-With","XMLHttpRequest"); //Otherwise we get a full page

            var data = client.DownloadData(endPoint);
            var stringData = Encoding.UTF8.GetString(data);
        }
    }
}
