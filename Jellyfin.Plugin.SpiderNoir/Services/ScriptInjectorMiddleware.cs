using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.SpiderNoir.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SpiderNoir.Services;

/// <summary>
/// ASP.NET Core middleware that injects the SpiderNoir player overlay script
/// into HTML responses served by Jellyfin.
/// </summary>
/// <remarks>
/// This replaces the old Custom JavaScript field (removed in Jellyfin 10.9+)
/// by injecting our script tag directly into the served index.html.
/// </remarks>
public class ScriptInjectorMiddleware
{
    private const string PluginConfigKey = "SpiderNoir";
    private readonly RequestDelegate _next;
    private readonly ILogger<ScriptInjectorMiddleware> _logger;
    private readonly IConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptInjectorMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationManager">The configuration manager.</param>
    public ScriptInjectorMiddleware(
        RequestDelegate next,
        ILogger<ScriptInjectorMiddleware> logger,
        IConfigurationManager configurationManager)
    {
        _next = next;
        _logger = logger;
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept HTML responses served to browser
        var path = context.Request.Path.Value;
        if (!string.IsNullOrEmpty(path)
            && !path.Equals("/", StringComparison.OrdinalIgnoreCase)
            && !path.Contains("index.html", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/web/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Check if overlay is enabled in configuration
        var config = _configurationManager.GetConfiguration<PluginConfiguration>(PluginConfigKey);
        if (!config.EnablePlayerOverlay)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Buffer the response to inject our script
        var originalBody = context.Response.Body;
        using var memoryStream = new MemoryStream();

        context.Response.Body = memoryStream;

        try
        {
            await _next(context).ConfigureAwait(false);

            // Only inject into HTML responses
            var contentType = context.Response.ContentType ?? string.Empty;
            if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBody).ConfigureAwait(false);
                return;
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync().ConfigureAwait(false);

            // Inject script tag before </head>
            if (responseBody.Contains("</head>", StringComparison.OrdinalIgnoreCase))
            {
                responseBody = responseBody.Replace(
                    "</head>",
                    "<script src=\"/SpiderNoir/player-overlay.js\"></script>\n</head>",
                    StringComparison.OrdinalIgnoreCase);

                _logger.LogDebug("Injected SpiderNoir player overlay script into HTML response");
            }

            var bytes = Encoding.UTF8.GetBytes(responseBody);
            context.Response.ContentLength = bytes.Length;
            await originalBody.WriteAsync(bytes.AsMemory(), context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
