using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BoundlessProxyUi
{
    internal class Network
    {
        internal static HttpClient NidHttpClient { get; } = GetNidHttpClient();

        internal static string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        private static HttpClient GetNidHttpClient()
        {
            SocketsHttpHandler socketsHttpHandler = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
            ProductInfoHeaderValue nidBot = new("BoundlessProxyUIBot", AppVersion);
            ProductInfoHeaderValue userAgentComment = new($"(+https://discord.nidaren.net)");

            HttpClient httpClient = new(socketsHttpHandler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            httpClient.DefaultRequestHeaders.UserAgent.Add(nidBot);
            httpClient.DefaultRequestHeaders.UserAgent.Add(userAgentComment);

            return httpClient;
        }
    }
}
