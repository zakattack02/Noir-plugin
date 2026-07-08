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
/// into the Jellyfin web client's index.html.
/// </summary>
/// <remarks>
/// This replaces the old Custom JavaScript field (removed in Jellyfin 10.9+)
/// by injecting our script tag directly into the served index.html.
/// It reads the file from disk, modifies it, and serves it — bypassing
/// response compression so the HTML is always readable.
/// </remarks>
public class ScriptInjectorMiddleware
{
    private const string PluginConfigKey = "SpiderNoir";
    private const string ScriptSnippet = "<script src=\"/SpiderNoir/player-overlay.js\"></script>\n";

    private readonly RequestDelegate _next;
    private readonly ILogger<ScriptInjectorMiddleware> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly string _webPath;

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
        _webPath = configurationManager.CommonApplicationPaths?.WebPath ?? string.Empty;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept GET requests for the web client's index.html
        if (!IsIndexHtmlRequest(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(_webPath) || !Directory.Exists(_webPath))
        {
            _logger.LogWarning("Web path not found: {WebPath}", _webPath);
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Check if overlay is enabled in configuration
        try
        {
            var config = _configurationManager.GetConfiguration<PluginConfiguration>(PluginConfigKey);
            if (!config.EnablePlayerOverlay)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }
        }
        catch
        {
            // If we can't read config, fall through to normal serving
            await _next(context).ConfigureAwait(false);
            return;
        }

        try
        {
            var indexPath = Path.Combine(_webPath, "index.html");
            if (!File.Exists(indexPath))
            {
                _logger.LogWarning("index.html not found at {Path}", indexPath);
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Read the file and inject the script tag if needed
            var originalHtml = await File.ReadAllTextAsync(indexPath).ConfigureAwait(false);

            if (!originalHtml.Contains("</head>", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("index.html does not contain </head> tag");
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Only inject if not already injected
            string modifiedHtml;
            if (originalHtml.Contains("/SpiderNoir/player-overlay.js", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Script already present in index.html");
                modifiedHtml = originalHtml;
            }
            else
            {
                modifiedHtml = originalHtml.Replace(
                    "</head>",
                    ScriptSnippet + "</head>",
                    StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation("Injected SpiderNoir overlay script into index.html");
            }

            var bytes = Encoding.UTF8.GetBytes(modifiedHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength = bytes.Length;
            await context.Response.Body.WriteAsync(bytes.AsMemory(), context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject SpiderNoir script into index.html");
            await _next(context).ConfigureAwait(false);
        }
    }

    private static bool IsIndexHtmlRequest(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        // Match /, /web/, /web/index.html, /index.html
        return path.Equals("/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/web/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase);
    }
}
