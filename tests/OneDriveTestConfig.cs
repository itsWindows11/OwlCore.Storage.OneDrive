using Microsoft.Extensions.Configuration;

namespace OwlCore.Storage.OneDrive.Tests;

public static class OneDriveTestConfig
{
    private static IConfigurationRoot? _config;

    public static string GetAccessToken()
    {
        if (_config == null)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<OneDriveFolderTests>()
                .AddEnvironmentVariables();
            _config = builder.Build();
        }

        return _config["OC_ONEDRIVE_TOKEN"] ?? string.Empty;
    }
}
