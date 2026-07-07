using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SpiderNoir.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SpiderNoir.Services;

/// <summary>
/// Service that detects SpiderNoir episodes with both Noir and Color versions.
/// </summary>
public class VersionDetectionService
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<VersionDetectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionDetectionService"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{VersionDetectionService}"/> interface.</param>
    public VersionDetectionService(ILibraryManager libraryManager, ILogger<VersionDetectionService> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>
    /// Scans the library for SpiderNoir episodes that have both Noir and Color versions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of episode version pairs.</returns>
    public Task<IReadOnlyList<EpisodeVersionPair>> DetectVersionPairsAsync(CancellationToken cancellationToken = default)
    {
        var pairs = new List<EpisodeVersionPair>();
        var config = Config;

        var seriesList = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            Recursive = true
        });

        var matchingSeries = seriesList
            .Where(s => s.Name is not null && s.Name.Contains(config.SeriesNamePattern, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _logger.LogInformation("Found {Count} series matching pattern '{Pattern}'", matchingSeries.Count, config.SeriesNamePattern);

        foreach (var series in matchingSeries)
        {
            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Episode],
                Recursive = true,
                ParentId = series.Id
            });

            foreach (var item in episodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item is not Episode episode || string.IsNullOrEmpty(episode.Path))
                {
                    continue;
                }

                var pair = DetectVersionPair(episode, config);
                if (pair is not null)
                {
                    pairs.Add(pair);
                }
            }
        }

        _logger.LogInformation("Detected {Count} episode version pairs", pairs.Count);
        return Task.FromResult<IReadOnlyList<EpisodeVersionPair>>(pairs);
    }

    /// <summary>
    /// Detects the version pair for a single episode.
    /// </summary>
    /// <param name="episode">The episode to check.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>A version pair if both versions exist, otherwise null.</returns>
    public EpisodeVersionPair? DetectVersionPair(Episode episode, PluginConfiguration? config = null)
    {
        config ??= Config;
        var episodePath = episode.Path;

        if (string.IsNullOrEmpty(episodePath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(episodePath);
        var fileName = Path.GetFileNameWithoutExtension(episodePath);
        var extension = Path.GetExtension(episodePath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(extension))
        {
            return null;
        }

        return config.VersionNaming switch
        {
            VersionNamingConvention.SuffixBased => DetectSuffixBasedPair(directory, fileName, extension, episode, config),
            VersionNamingConvention.FolderBased => DetectFolderBasedPair(directory, fileName, extension, episode, config),
            _ => null
        };
    }

    /// <summary>
    /// Attempts to retrieve the version pair for a given item id.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>The version pair if found, otherwise null.</returns>
    public EpisodeVersionPair? GetPairForItem(Guid itemId)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is not Episode episode)
        {
            return null;
        }

        return DetectVersionPair(episode);
    }

    /// <summary>
    /// Gets the alternate version path (the version NOT currently being played).
    /// </summary>
    /// <param name="currentPath">The current file path.</param>
    /// <returns>The alternate version path, or null if not found.</returns>
    public string? GetAlternateVersionPath(string currentPath)
    {
        var config = Config;
        var directory = Path.GetDirectoryName(currentPath);
        var fileName = Path.GetFileNameWithoutExtension(currentPath);
        var extension = Path.GetExtension(currentPath);

        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(extension))
        {
            return null;
        }

        var isNoir = fileName.Contains(config.NoirSuffix, StringComparison.OrdinalIgnoreCase)
                     || fileName.Contains("B&W", StringComparison.OrdinalIgnoreCase);

        string? alternatePath;
        var separators = new[] { " - ", " " };

        if (isNoir)
        {
            var baseName = StripSuffix(fileName, config.NoirSuffix, separators);
            alternatePath = Path.Combine(directory, baseName + " - " + config.ColorSuffix + extension);
            if (!File.Exists(alternatePath))
            {
                alternatePath = Path.Combine(directory, baseName + " " + config.ColorSuffix + extension);
                if (!File.Exists(alternatePath))
                {
                    alternatePath = Path.Combine(directory, baseName + extension);
                }
            }
        }
        else
        {
            var baseName = StripSuffix(fileName, config.ColorSuffix, separators);
            alternatePath = Path.Combine(directory, baseName + " - " + config.NoirSuffix + extension);
            if (!File.Exists(alternatePath))
            {
                alternatePath = Path.Combine(directory, baseName + " " + config.NoirSuffix + extension);
                if (!File.Exists(alternatePath))
                {
                    alternatePath = Path.Combine(directory, baseName + " - B&W" + extension);
                    if (!File.Exists(alternatePath))
                    {
                        alternatePath = Path.Combine(directory, baseName + " B&W" + extension);
                    }
                }
            }
        }

        return File.Exists(alternatePath) ? alternatePath : null;
    }

    private static EpisodeVersionPair? DetectSuffixBasedPair(
        string directory,
        string fileName,
        string extension,
        Episode episode,
        PluginConfiguration config)
    {
        var separators = new[] { " - ", " " };
        var isNoirTagged = fileName.Contains(config.NoirSuffix, StringComparison.OrdinalIgnoreCase)
                           || fileName.Contains("B&W", StringComparison.OrdinalIgnoreCase);
        var isColorTagged = fileName.Contains(config.ColorSuffix, StringComparison.OrdinalIgnoreCase);

        string? noirPath = null;
        string? colorPath = null;

        if (isNoirTagged)
        {
            noirPath = episode.Path;
            var baseName = StripSuffix(fileName, config.NoirSuffix, separators);
            colorPath = FindSiblingFile(directory, baseName, extension, null);
            if (colorPath is null)
            {
                baseName = StripSuffix(fileName, "B&W", separators);
                colorPath = FindSiblingFile(directory, baseName, extension, null);
            }
        }
        else if (isColorTagged)
        {
            colorPath = episode.Path;
            var baseName = StripSuffix(fileName, config.ColorSuffix, separators);
            noirPath = FindSiblingFile(directory, baseName, extension, config.NoirSuffix)
                       ?? FindSiblingFile(directory, baseName, extension, "B&W");
        }
        else
        {
            colorPath = episode.Path;
            noirPath = FindSiblingFile(directory, fileName, extension, config.NoirSuffix)
                       ?? FindSiblingFile(directory, fileName, extension, "B&W");
        }

        if (noirPath is not null && colorPath is not null && noirPath != colorPath)
        {
            return new EpisodeVersionPair
            {
                EpisodeId = episode.Id,
                EpisodeName = episode.Name,
                SeriesName = episode.SeriesName,
                NoirPath = noirPath,
                ColorPath = colorPath,
                IsCurrentNoir = isNoirTagged
            };
        }

        return null;
    }

    private static EpisodeVersionPair? DetectFolderBasedPair(
        string directory,
        string fileName,
        string extension,
        Episode episode,
        PluginConfiguration config)
    {
        var parentDir = Path.GetDirectoryName(directory);
        if (string.IsNullOrEmpty(parentDir))
        {
            return null;
        }

        var currentFolderName = Path.GetFileName(directory) ?? string.Empty;
        var isNoirFolder = currentFolderName.Contains(config.NoirSuffix, StringComparison.OrdinalIgnoreCase)
                           || currentFolderName.Contains("Noir", StringComparison.OrdinalIgnoreCase);
        var isColorFolder = currentFolderName.Contains(config.ColorSuffix, StringComparison.OrdinalIgnoreCase);

        string? noirPath = null;
        string? colorPath = null;

        if (isNoirFolder)
        {
            noirPath = episode.Path;
            var colorDir = Path.Combine(parentDir, "Color");
            colorPath = Path.Combine(colorDir, fileName + extension);
            if (!File.Exists(colorPath))
            {
                colorPath = null;
            }
        }
        else if (isColorFolder)
        {
            colorPath = episode.Path;
            var noirDir = Path.Combine(parentDir, "Noir");
            noirPath = Path.Combine(noirDir, fileName + extension);
            if (!File.Exists(noirPath))
            {
                noirPath = Path.Combine(parentDir, "B&W", fileName + extension);
                noirPath = File.Exists(noirPath) ? noirPath : null;
            }
        }

        if (noirPath is not null && colorPath is not null)
        {
            return new EpisodeVersionPair
            {
                EpisodeId = episode.Id,
                EpisodeName = episode.Name,
                SeriesName = episode.SeriesName,
                NoirPath = noirPath,
                ColorPath = colorPath,
                IsCurrentNoir = isNoirFolder
            };
        }

        return null;
    }

    private static string? FindSiblingFile(string directory, string baseName, string extension, string? suffix)
    {
        if (!string.IsNullOrEmpty(suffix))
        {
            var taggedPath = Path.Combine(directory, baseName + " - " + suffix + extension);
            if (File.Exists(taggedPath))
            {
                return taggedPath;
            }

            taggedPath = Path.Combine(directory, baseName + " " + suffix + extension);
            if (File.Exists(taggedPath))
            {
                return taggedPath;
            }
        }

        var exactPath = Path.Combine(directory, baseName + extension);
        return File.Exists(exactPath) ? exactPath : null;
    }

    private static string StripSuffix(string fileName, string suffix, string[] separators)
    {
        foreach (var sep in separators)
        {
            var pattern = sep + suffix;
            if (fileName.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^pattern.Length];
            }
        }

        return fileName;
    }
}
