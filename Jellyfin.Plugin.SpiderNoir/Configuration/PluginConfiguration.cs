using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SpiderNoir.Configuration;

/// <summary>
/// Naming conventions for identifying version pairs.
/// </summary>
public enum VersionNamingConvention
{
    /// <summary>
    /// Suffix-based: e.g. "S01E01 - Noir" vs "S01E01".
    /// </summary>
    SuffixBased,

    /// <summary>
    /// Folder-based: episodes in separate "Color" and "Noir" subdirectories.
    /// </summary>
    FolderBased,

    /// <summary>
    /// Custom pattern defined by the user.
    /// </summary>
    Custom
}

/// <summary>
/// Plugin configuration for SpiderNoir.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        VersionNaming = VersionNamingConvention.SuffixBased;
        NoirSuffix = "Noir";
        ColorSuffix = "Color";
        AutoDetectSpiderNoirSeries = true;
        SeriesNamePattern = "SpiderNoir";
        EnablePlayerOverlay = true;
    }

    /// <summary>
    /// Gets or sets the naming convention used to identify Noir/Color version pairs.
    /// </summary>
    public VersionNamingConvention VersionNaming { get; set; }

    /// <summary>
    /// Gets or sets the file suffix that identifies the Noir version.
    /// </summary>
    public string NoirSuffix { get; set; }

    /// <summary>
    /// Gets or sets the file suffix/tag that identifies the Color version.
    /// </summary>
    public string ColorSuffix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to auto-detect SpiderNoir series by name.
    /// </summary>
    public bool AutoDetectSpiderNoirSeries { get; set; }

    /// <summary>
    /// Gets or sets the series name pattern to match for auto-detection.
    /// </summary>
    public string SeriesNamePattern { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the version switch overlay in the player.
    /// </summary>
    public bool EnablePlayerOverlay { get; set; }
}
