using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Jellyfin.Plugin.SpiderNoir.Services;

/// <summary>
/// Startup filter that adds the SpiderNoir script injection middleware
/// to the Jellyfin web server pipeline.
/// </summary>
public class ScriptInjectionStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseMiddleware<ScriptInjectorMiddleware>();
            next(app);
        };
    }
}
