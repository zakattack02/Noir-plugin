using System;

namespace Jellyfin.Plugin.SpiderNoir.Services;

/// <summary>
/// Represents a detected version pair for an episode.
/// </summary>
public class EpisodeVersionPair
{
    /// <summary>
    /// Gets or sets the episode identifier.
    /// </summary>
    public Guid EpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the episode name.
    /// </summary>
    public string? EpisodeName { get; set; }

    /// <summary>
    /// Gets or sets the series name.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the path to the Noir (black and white) version.
    /// </summary>
    public string? NoirPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the Color version.
    /// </summary>
    public string? ColorPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the currently playing version is Noir.
    /// </summary>
    public bool IsCurrentNoir { get; set; }
}
