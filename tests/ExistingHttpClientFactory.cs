using Microsoft.Identity.Client;

namespace OwlCore.Storage.OneDrive.Tests
{
    public partial class TestEnv
    {
        public class ExistingHttpClientFactory : IMsalHttpClientFactory
        {
            private readonly HttpClient client;

            public ExistingHttpClientFactory(HttpClient client)
            {
                this.client = client;
            }

            public HttpClient GetHttpClient()
            {
                return this.client;
            }
        }
    }
}
