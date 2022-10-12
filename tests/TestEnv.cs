using Azure.Core;
using dotenv.net;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using OwlCore.Net.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace OwlCore.Storage.OneDrive.Tests
{
    [TestClass]
    public partial class TestEnv
    {
        private string _httpResponseCachePath = Path.Combine(Environment.CurrentDirectory, "OwlCore.Storage.OneDrive", "HttpResponseCache");
        private string? _authorityUri;
        private readonly string[] _scopes = { "Files.Read.All", "User.Read", "Files.ReadWrite" };

        public TestEnv()
        {
            try
            {
                var envVars = DotEnv.Fluent()
                    .WithExceptions()
                    .WithEnvFiles() // revert to the default .env file
                    .WithTrimValues()
                    .WithDefaultEncoding()
                    .WithOverwriteExistingVars()
                    .WithProbeForEnv()
                    .Read();

                ClientId = envVars[nameof(ClientId)];
                TenantId = envVars[nameof(TenantId)];
                RedirectUri = envVars[nameof(RedirectUri)];

                if (!envVars.TryGetValue(nameof(AuthorityUri), out _authorityUri))
                    _authorityUri = "https://login.microsoftonline.com/consumers";
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to load test environment. Make sure a .env file is present with the required values. Error: {ex}");
            }
        }

        public string ClientId { get; private set; }

        public string TenantId { get; private set; }

        public string RedirectUri { get; private set; }

        public string AuthorityUri => _authorityUri ?? throw new InvalidOperationException($"{nameof(AuthorityUri)} was not found. Check your environment config.");

        public async Task<GraphServiceClient> CreateClientAsync()
        {
            var cachedMessageHandler = new CachedHttpClientHandler(_httpResponseCachePath, TimeSpan.MaxValue);
            cachedMessageHandler.CachedRequestSaving += CachedMessageHandler_CachedRequestSaving;

            // Authentication result needs to be cached
            // So that the library creates the same URLs internally. Cache name is based on URL.
            var authenticationResult = await AcquireTokenAsync(new HttpClient(cachedMessageHandler));
            var authProvider = await AuthenticateAsync(authenticationResult);

            // Acquire graph client
            var handlers = GraphClientFactory.CreateDefaultHandlers(authProvider);
            var httpClient = GraphClientFactory.Create(handlers, finalHandler: cachedMessageHandler);

            return new GraphServiceClient(httpClient);
        }

        // ================= TODO =================
        // Once CachedHttpClientHandler gets the needed improvements,
        // we'll use it to cache real data to disk, scrub sensitive data, commit to git, and use the data to mock API responses.
        private void CachedMessageHandler_CachedRequestSaving(object? sender, CachedRequestEventArgs e)
        {
            if (e.CacheEntry.ContentBytes is null)
                return;

            var str = Encoding.UTF8.GetString(e.CacheEntry.ContentBytes);

            str = str.Replace(' ', ' '); // TODO, replace sensitive information

            // TODO Replace original with changed copy.
            e.CacheEntry.ContentBytes = Encoding.UTF8.GetBytes(str);
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(HttpClient httpClient)
        {
            // Acquire token
            var authority = new Uri($"{AuthorityUri}/{TenantId}");

            var clientBuilder = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithHttpClientFactory(new ExistingHttpClientFactory(httpClient))
                .WithAuthority(authority);

            if (!string.IsNullOrWhiteSpace(RedirectUri))
                clientBuilder.WithRedirectUri(RedirectUri);

            var tokenBuilder = clientBuilder.Build().AcquireTokenWithDeviceCode(_scopes, x =>
            {
                if (!Debugger.IsAttached)
                    Assert.Fail($"Due to the login requirement, tests must be run locally and with the debugger attached. Please attach the debugger and try again");
                else
                    Debug.WriteLine($"A login is required before tests can be run. Please go to {x.VerificationUrl} and enter the code {x.UserCode}.");

                return Task.CompletedTask;
            });

            var authenticationResult = await tokenBuilder.ExecuteAsync();

            return authenticationResult;
        }

        private async Task<IAuthenticationProvider> AuthenticateAsync(AuthenticationResult authenticationResult)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(authenticationResult.AccessToken));

            return new DelegateAuthenticationProvider(requestMessage =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authenticationResult.AccessToken);
                return Task.CompletedTask;
            });
        }
    }
}
