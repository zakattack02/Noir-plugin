using Jellyfin.Plugin.SpiderNoir.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SpiderNoir.Services;

/// <summary>
/// Registers SpiderNoir services in the Jellyfin dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<VersionDetectionService>(serviceProvider =>
        {
            var libraryManager = serviceProvider.GetRequiredService<MediaBrowser.Controller.Library.ILibraryManager>();
            var logger = serviceProvider.GetRequiredService<ILogger<VersionDetectionService>>();
            return new VersionDetectionService(libraryManager, logger);
        });

        // Register middleware that auto-injects the player overlay script
        // into HTML responses (replaces the removed Custom JS field)
        serviceCollection.AddSingleton<IStartupFilter, ScriptInjectionStartupFilter>();
    }
}
