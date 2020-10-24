using System;
using System.Net.Http;

namespace AzureUtilities.DataCleanup.Shared
{
    public sealed class ClientFixture : IDisposable
    {
        private HttpClient _client;

        public HttpClient GetClient()
        {
            if (_client != null)
            {
                return _client;
            }
            else
            {
                _client = new HttpClient()
                {
                    Timeout = TimeSpan.FromSeconds(120)
                };
            }

            return _client;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }
    }
}
