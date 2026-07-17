namespace Shad360.Core.Models;

/// <summary>
/// Represents the different release types for shad360 emulator and manager
/// </summary>
public enum ReleaseType
{
    /// <summary>
    /// shad360 main emulator release
    /// </summary>
    Shad360,

    /// <summary>
    /// shad360 Manager stable release
    /// </summary>
    Shad360Stable,

    /// <summary>
    /// shad360 Manager experimental/nightly release
    /// </summary>
    Shad360Experimental
}