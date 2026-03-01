using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions.Authentication;

namespace OwlCore.Storage.OneDrive.Tests;

public class TestAccessTokenProvider : IAccessTokenProvider
{
    private readonly string _token;

    public TestAccessTokenProvider(string token)
    {
        _token = token;
    }

    public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = default, CancellationToken cancellationToken = default) 
        => Task.FromResult(_token);

    public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
}
