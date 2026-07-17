using System.Text.Json.Serialization;
using NLog;
using Shad360.Core.Converters;

namespace Shad360.Core.Settings.Sections;

/// <summary>
/// Settings related to debugging and logging
/// </summary>
public class DebugSettings
{
    /// <summary>
    /// The minimum logging level for log output
    /// </summary>
    [JsonPropertyName("log_level")]
    [JsonConverter(typeof(LogLevelJsonConverter))]
#if EXPERIMENTAL_BUILD
    public LogLevel LogLevel { get; set; } = LogLevel.Trace;
#else
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
#endif
}
