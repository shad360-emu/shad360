using System.Text.Json.Serialization;

namespace Shad360.Core.Models;

/// <summary>
/// Represents cached build information for a specific release
/// </summary>
public class CachedBuild
{
    /// <summary>
    /// The tag name of the release (e.g., "v1.0.0")
    /// </summary>
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// The date of the release
    /// </summary>
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// The URL to the release
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    public CachedBuild() { }

    public CachedBuild(string tagName, DateTime date, string url)
    {
        TagName = tagName;
        Date = date;
        Url = url;
    }
}

/// <summary>
/// Represents cached build information for shad360 Manager releases
/// </summary>
public class ManagerBuild
{
    /// <summary>
    /// The version string (tag name)
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The URL to the release
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    public ManagerBuild() { }

    public ManagerBuild(string version, string url)
    {
        Version = version;
        Url = url;
    }
}

/// <summary>
/// Represents the complete manifest cache containing all release information
/// </summary>
public class ReleaseCache
{
    /// <summary>
    /// shad360 main emulator release
    /// </summary>
    [JsonPropertyName("shad360")]
    public CachedBuild? Shad360 { get; set; }

    /// <summary>
    /// shad360 Manager stable release
    /// </summary>
    [JsonPropertyName("shad360_stable")]
    public ManagerBuild? Shad360Stable { get; set; }

    /// <summary>
    /// shad360 Manager experimental release
    /// </summary>
    [JsonPropertyName("shad360_experimental")]
    public ManagerBuild? Shad360Experimental { get; set; }
}