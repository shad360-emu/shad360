using System.Text.Json.Serialization;

namespace Shad360.Core.Settings.Sections;

/// <summary>
/// Represents the settings configuration for the shad360 emulator
/// </summary>
public class EmulatorSettings
{
    /// <summary>
    /// General emulator settings
    /// </summary>
    [JsonPropertyName("settings")]
    public GeneralEmulatorSettings Settings { get; set; } = new GeneralEmulatorSettings();

    /// <summary>
    /// Gets or sets information about the shad360 emulator
    /// </summary>
    [JsonPropertyName("shad360")]
    public EmulatorInfo? Shad360 { get; set; }
}

/// <summary>
/// Contains general settings for the emulator
/// </summary>
public class GeneralEmulatorSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the unified content folder
    /// </summary>
    [JsonPropertyName("unified_content")]
    public bool UnifiedContentFolder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether XConfig is enabled
    /// </summary>
    [JsonPropertyName("xconfig_enabled")]
    public bool XConfigEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether XConfig settings are shown
    /// </summary>
    [JsonPropertyName("xconfig_show")]
    public bool XConfigShow { get; set; } = true;
}

/// <summary>
/// Represents information about an installed emulator version
/// </summary>
public class EmulatorInfo
{
    /// <summary>
    /// The version string of the emulator
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.0.0";

    /// <summary>
    /// Whether this is a nightly build
    /// </summary>
    [JsonPropertyName("is_nightly")]
    public bool IsNightly { get; set; } = false;

    /// <summary>
    /// The last update check date
    /// </summary>
    [JsonPropertyName("last_update_check")]
    public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Whether an update is available
    /// </summary>
    [JsonPropertyName("update_available")]
    public bool UpdateAvailable { get; set; } = false;

    /// <summary>
    /// The latest version available
    /// </summary>
    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = string.Empty;
}