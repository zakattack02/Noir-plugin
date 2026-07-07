using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SpiderNoir.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SpiderNoir.Api;

/// <summary>
/// API controller for SpiderNoir version switching functionality.
/// </summary>
[ApiController]
[Authorize]
[Route("[controller]")]
public class SpiderNoirController : ControllerBase
{
    private readonly VersionDetectionService _versionDetectionService;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<SpiderNoirController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpiderNoirController"/> class.
    /// </summary>
    /// <param name="versionDetectionService">The version detection service.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public SpiderNoirController(
        VersionDetectionService versionDetectionService,
        ILibraryManager libraryManager,
        ILogger<SpiderNoirController> logger)
    {
        _versionDetectionService = versionDetectionService;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets version information for a specific item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <returns>Version information including available versions and current version.</returns>
    [HttpGet("versions/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetVersions([FromRoute] Guid itemId)
    {
        _logger.LogDebug("GetVersions called for item {ItemId}", itemId);

        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            return NotFound(new { error = "Item not found" });
        }

        var pair = _versionDetectionService.GetPairForItem(itemId);
        if (pair == null)
        {
            return Ok(new
            {
                hasVersions = false,
                currentVersion = "unknown",
                versions = Array.Empty<string>(),
                message = "No alternate versions found for this item."
            });
        }

        var versions = new List<object>();
        if (!string.IsNullOrEmpty(pair.ColorPath))
        {
            versions.Add(new { id = "color", label = "Color", path = pair.ColorPath });
        }

        if (!string.IsNullOrEmpty(pair.NoirPath))
        {
            versions.Add(new { id = "noir", label = "Noir (B&W)", path = pair.NoirPath });
        }

        return Ok(new
        {
            hasVersions = true,
            itemName = pair.EpisodeName,
            seriesName = pair.SeriesName,
            currentVersion = pair.IsCurrentNoir ? "noir" : "color",
            versions
        });
    }

    /// <summary>
    /// Gets the alternate version path to switch to.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="targetVersion">The target version ("noir" or "color").</param>
    /// <returns>The path to the target version.</returns>
    [HttpPost("switch/{itemId}/{targetVersion}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult SwitchVersion([FromRoute] Guid itemId, [FromRoute] string targetVersion)
    {
        _logger.LogInformation("SwitchVersion called for item {ItemId} to {TargetVersion}", itemId, targetVersion);

        if (targetVersion != "noir" && targetVersion != "color")
        {
            return BadRequest(new { error = "Invalid target version. Must be 'noir' or 'color'." });
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            return NotFound(new { error = "Item not found" });
        }

        var pair = _versionDetectionService.GetPairForItem(itemId);
        if (pair == null)
        {
            return NotFound(new { error = "No version pair found for this item." });
        }

        var targetPath = targetVersion == "noir" ? pair.NoirPath : pair.ColorPath;

        if (string.IsNullOrEmpty(targetPath))
        {
            return NotFound(new { error = $"Target version '{targetVersion}' not available for this item." });
        }

        return Ok(new
        {
            success = true,
            targetVersion,
            targetPath,
            itemId
        });
    }

    /// <summary>
    /// Lists all detected SpiderNoir series that have dual-version episodes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of series with their version information.</returns>
    [HttpGet("series")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSeries([FromQuery] CancellationToken cancellationToken)
    {
        _logger.LogDebug("GetSeries called");

        var pairs = await _versionDetectionService.DetectVersionPairsAsync(cancellationToken).ConfigureAwait(false);

        var seriesGroups = pairs
            .GroupBy(p => p.SeriesName)
            .Select(g => new
            {
                seriesName = g.Key,
                episodeCount = g.Count(),
                episodes = g.Select(e => new
                {
                    episodeId = e.EpisodeId,
                    episodeName = e.EpisodeName,
                    hasNoir = !string.IsNullOrEmpty(e.NoirPath),
                    hasColor = !string.IsNullOrEmpty(e.ColorPath)
                })
            })
            .ToList();

        return Ok(new
        {
            totalSeries = seriesGroups.Count,
            totalEpisodes = pairs.Count,
            series = seriesGroups
        });
    }
}
