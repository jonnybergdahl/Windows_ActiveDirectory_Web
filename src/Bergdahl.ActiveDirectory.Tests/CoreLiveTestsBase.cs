using Bergdahl.ActiveDirectory.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Bergdahl.ActiveDirectory.Tests;

public class CoreLiveTestsBase
{
    protected readonly ApplicationSettings Settings;
    protected readonly bool IsLiveEnvironment;

    public CoreLiveTestsBase()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        Settings = new ApplicationSettings();
        config.GetSection("ApplicationSettings").Bind(Settings);

        // Simple heuristic to check if we are in a live environment
        // In a real scenario, we might use an environment variable
        IsLiveEnvironment = !string.IsNullOrEmpty(Settings.ServerName) && 
                            Settings.ServerName != "dc01.example.com";
    }
}
