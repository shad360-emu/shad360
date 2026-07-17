namespace Shad360.Core.Constants;

/// <summary>
/// Contains all URLs used throughout the application
/// Organized by category for easy maintenance and updates
/// </summary>
public class Urls
{
    /// <summary>
    /// Contains base URLs used throughout the application
    /// </summary>
    public static class Base
    {
        /// <summary>
        /// Base URL for the shad360 GitHub Pages site
        /// Used as the primary source for various resources
        /// </summary>
        public const string GITHUB_PAGES = "https://shad360-emu.github.io";

        /// <summary>
        /// Base URL for raw GitHub content
        /// Used as an alternative source for resources hosted on GitHub
        /// </summary>
        public const string GITHUB_RAW = "https://raw.githubusercontent.com";
    }

    /// <summary>
    /// Array of URLs to fetch the "version.json" file containing information about latest releases of shad360 & shad360 Manager
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/database/data/version.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/shad360-emu/database/main/data/version.json)
    /// </summary>
    public static readonly string[] Manifest =
    [
        $"{Base.GITHUB_PAGES}/database/data/version.json",
        $"{Base.GITHUB_RAW}/shad360-emu/database/main/data/version.json"
    ];

    /// <summary>
    /// Array of URLs to fetch the "gamecontrollerdb.txt" file containing game controller mappings for shad360 SDL
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source
    /// 2. Raw GitHub - Fallback source
    /// </summary>
    public static readonly string[] GameControllerDatabase =
    [
        $"{Base.GITHUB_PAGES}/database/data/gamecontrollerdb.txt",
        $"{Base.GITHUB_RAW}/shad360-emu/database/main/data/gamecontrollerdb.txt"
    ];

    /// <summary>
    /// Array of URLs to fetch the Xbox Marketplace games database.
    /// This database contains information about Xbox 360 games and is used by the application
    /// to retrieve game details and metadata. Multiple URLs are provided to ensure availability,
    /// with fallback options in case the primary source is not reachable.
    /// Sources include:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/x360db/games.json)
    /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/shad360-emu/x360db/main/games.json)
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabase =
    [
        $"{Base.GITHUB_PAGES}/x360db/games.json",
        $"{Base.GITHUB_RAW}/shad360-emu/x360db/main/games.json"
    ];

    /// <summary>
    /// Array of URLs to fetch detailed game information from the Xbox marketplace database
    /// These are format strings with {0} as a placeholder for the title ID
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/x360db/titles/{0}/info.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/shad360-emu/x360db/main/titles/{0}/info.json)
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabaseGameInfo =
    [
        Base.GITHUB_PAGES + "/x360db/titles/{0}/info.json",
        Base.GITHUB_RAW + "/shad360-emu/x360db/main/titles/{0}/info.json"
    ];

    /// <summary>
    /// Array of URLs to fetch artwork files from the Xbox marketplace database
    /// These are format strings with {0} as a placeholder for the title ID and {1} as a placeholder for the artwork filename
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/x360db/titles/{0}/artwork/{1})
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/shad360-emu/x360db/main/titles/{0}/artwork/{1})
    /// </summary>
    public static readonly string[] XboxMarketplaceDatabaseArtwork =
    [
        Base.GITHUB_PAGES + "/x360db/titles/{0}/artwork/{1}",
        Base.GITHUB_RAW + "/shad360-emu/x360db/main/titles/{0}/artwork/{1}"
    ];

    /// <summary>
    /// Array of URLs to fetch the Game Compatibility database.
    /// This database contains information about game compatibility ratings with the emulator
    /// and is used by the application to retrieve compatibility status for games.
    /// Multiple URLs are provided to ensure availability, with fallback options in case
    /// the primary source is not reachable.
    /// Sources include:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/database/game-compatibility/shad360.json)
    /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/shad360-emu/database/main/data/game-compatibility/shad360.json)
    /// </summary>
    public static readonly string[] GameCompatibilityDatabase =
    [
        Base.GITHUB_PAGES + "/database/data/game-compatibility/shad360.json",
        Base.GITHUB_RAW + "/shad360-emu/database/main/data/game-compatibility/shad360.json"
    ];

    /// <summary>
    /// Array of URLs to fetch list of optimized settings
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/optimized-settings/data/settings.json)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/shad360-emu/optimized-settings/main/data/settings.json)
    /// </summary>
    public static readonly string[] OptimizedSettingsDatabase =
    [
        $"{Base.GITHUB_PAGES}/optimized-settings/data/settings.json",
        $"{Base.GITHUB_RAW}/shad360-emu/optimized-settings/main/data/settings.json"
    ];

    /// <summary>
    /// Array of base URLs to fetch optimized settings for specific games
    /// Multiple URLs are provided as fallbacks in case the primary source is unavailable
    /// The application will attempt to fetch from the first URL, and if that fails,
    /// it will try the later URLs in order
    ///
    /// URLs included:
    /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/optimized-settings/settings/)
    /// 2. Raw GitHub - Fallback source (https://raw.githubusercontent.com/shad360-emu/optimized-settings/main/settings/)
    /// </summary>
    public static readonly string[] BaseOptimizedSettingsUrl =
    [
        $"{Base.GITHUB_PAGES}/optimized-settings/settings/",
        $"{Base.GITHUB_RAW}/shad360-emu/optimized-settings/main/settings/"
    ];

    /// <summary>
    /// Contains URLs to fetch the Patches database.
    /// This database contains patch files for shad360 emulator games and is used by the application
    /// to retrieve and apply game patches. Multiple URLs are provided to ensure availability,
    /// with fallback options in case the primary source is not reachable.
    /// Sources include:
    /// 1. GitHub Pages - Primary source
    /// 2. Raw GitHub - Secondary source
    /// </summary>
    public static class PatchesDatabase
    {
        /// <summary>
        /// Array of URLs to fetch the shad360 patches database.
        /// This database contains patch files for the shad360 version of the emulator.
        /// Multiple URLs are provided to ensure availability, with fallback options in case
        /// the primary source is not reachable.
        /// Sources include:
        /// 1. GitHub Pages - Primary source (https://shad360-emu.github.io/database/data/patches/shad360.json)
        /// 2. Raw GitHub - Secondary source (https://raw.githubusercontent.com/shad360-emu/database/main/data/patches/shad360.json)
        /// </summary>
        public static readonly string[] Shad360Patches =
        [
            Base.GITHUB_PAGES + "/database/data/patches/shad360.json",
            Base.GITHUB_RAW + "/shad360-emu/database/main/data/patches/shad360.json"
        ];
    }
}