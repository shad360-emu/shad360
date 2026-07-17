using Shad360.Core.Logging;
using Shad360.Core.Manage;
using Shad360.Core.Models;
using Shad360.Core.Models.Files.Account;
using Shad360.Core.Models.Game;
using Shad360.Core.Settings.Sections;
using Shad360.Core.Utilities;
namespace Shad360.Core.Installation;

/// <summary>
/// Service class responsible for setting up and configuring the shad360 emulator
/// </summary>
public class XeniaService
{
    /// <summary>
    /// Sets up a shad360 emulator installation with the specified version and configuration
    /// </summary>
    /// <param name="releaseVersion">The release version string to associate with this installation</param>
    /// <param name="unifiedContentFolder">Flag indicating whether to use a unified content folder structure</param>
    /// <returns>An EmulatorInfo object containing all the necessary information about the installed emulator</returns>
    /// <exception cref="FileNotFoundException">Thrown when the generated config file cannot be found after creation</exception>
    public static EmulatorInfo SetupEmulator(string releaseVersion, bool unifiedContentFolder = false)
    {
        // Perform the necessary registry setup for shad360
        InstallationHelper.RegistrySetup();

        // Create the emulator info object with initial configuration paths
        EmulatorInfo emulatorInfo = new EmulatorInfo
        {
            EmulatorLocation = EmulatorPaths.EmulatorDir,
            ExecutableLocation = EmulatorPaths.ExecutableLocation,
            ConfigLocation = EmulatorPaths.DefaultConfigLocation,
            Version = releaseVersion
        };

        // Setup the emulator directory structure
        InstallationHelper.SetupEmulatorDirectory(AppPathResolver.GetFullPath(emulatorInfo.EmulatorLocation));

        // Create a unified content folder if needed
        if (unifiedContentFolder)
        {
            InstallationHelper.SetupContentFolder(AppPathResolver.GetFullPath(EmulatorPaths.ContentFolderLocation));
        }

        Logger.Info<XeniaService>($"Checking for existing profiles in content folder for shad360");

        // Check if there are existing profiles in the content folder
        List<AccountInfo> profiles = ProfileManager.LoadProfiles();
        Logger.Info<XeniaService>($"Found {profiles.Count} existing profiles for shad360");

        bool shouldGenerateProfile = true; // Only generate a profile if none exist
        if (profiles.Count <= 0)
        {
            Logger.Info<XeniaService>($"No existing profiles found. Creating a default account profile for shad360");

            try
            {
                // Generate a default account profile
                ProfileManager.CreateAccount("User");
                Logger.Info<XeniaService>($"Successfully created default account profile for shad360");

                // Since we just created a profile, we don't need to generate one during config generation
                shouldGenerateProfile = false;
            }
            catch (Exception ex)
            {
                Logger.Warning<XeniaService>($"Failed to create account for shad360: {ex.Message}");
                Logger.LogExceptionDetails<XeniaService>(ex);
                Logger.Warning<XeniaService>("Falling back to manual profile generation during config generation");
                shouldGenerateProfile = true; // Try to generate profile during config generation
            }
        }
        else
        {
            Logger.Info<XeniaService>($"Profiles already exist for shad360, skipping profile creation");
            shouldGenerateProfile = false;
        }

        // Generate the initial configuration file using the executable
        Logger.Info<XeniaService>($"Generating configuration file for shad360. Generate profile during config: {shouldGenerateProfile}");
        ConfigManager.GenerateEmulatorConfigurationFile(EmulatorVersion.Shad360, shouldGenerateProfile);

        // Update the config location in the emulator info to reflect the moved file
        emulatorInfo.ConfigLocation = EmulatorPaths.ConfigLocation;

        // Set up the working configuration file with the default emulator one
        ConfigManager.ChangeConfigurationFile(AppPathResolver.GetFullPath(emulatorInfo.ConfigLocation), EmulatorVersion.Shad360);

        return emulatorInfo;
    }

    /// <summary>
    /// Checks for updates for the shad360 emulator
    /// </summary>
    /// <param name="releaseService">The release service used to fetch the latest build information</param>
    /// <param name="emulatorInfo">The current emulator information containing the installed version</param>
    /// <param name="forceRefresh">If true, forces a refresh of the release cache before checking for updates</param>
    /// <returns>A tuple containing (isUpdateAvailable, latestVersion) - true if the update is available along with the latest version string</returns>
    public static async Task<(bool IsUpdateAvailable, string LatestVersion)> CheckForUpdatesAsync(IReleaseService releaseService, EmulatorInfo emulatorInfo, bool forceRefresh = false)
    {
        Logger.Info<XeniaService>($"Checking for updates for shad360");

        try
        {
            // Force refresh if requested
            if (forceRefresh)
            {
                Logger.Info<XeniaService>("Forcing release cache refresh before checking for updates");
                await releaseService.ForceRefreshAsync();
            }

            // Fetch the latest release information
            CachedBuild? releaseBuild = await releaseService.GetCachedBuildAsync(ReleaseType.Shad360);
            if (releaseBuild == null)
            {
                Logger.Warning<XeniaService>($"Failed to fetch release information for shad360");
                return (false, string.Empty);
            }

            // Get current and latest versions
            string currentVersion = emulatorInfo.UseNightlyBuild ? emulatorInfo.NightlyVersion : emulatorInfo.Version;
            string latestVersion = releaseBuild.TagName;

            // Check if there's a new version
            bool isUpdateAvailable = latestVersion != currentVersion;

            Logger.Info<XeniaService>(isUpdateAvailable ? $"Update available: {currentVersion} -> {latestVersion}" : "Emulator is up to date");

            return (isUpdateAvailable, latestVersion);
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaService>($"Failed to check for updates: {ex.Message}");
            Logger.LogExceptionDetails<XeniaService>(ex);
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Updates the emulator information with the new release version
    /// </summary>
    /// <param name="emulatorInfo">The current emulator information to update, or null to perform fresh setup</param>
    /// <param name="releaseVersion">The new release version string to associate with this installation</param>
    public static void UpdateEmulator(EmulatorInfo? emulatorInfo, string releaseVersion)
    {
        emulatorInfo ??= SetupEmulator(releaseVersion);
        emulatorInfo.Version = emulatorInfo.UseNightlyBuild ? "v0.0.0" : releaseVersion;
        emulatorInfo.NightlyVersion = emulatorInfo.UseNightlyBuild ? releaseVersion : "v0.0.0";
        emulatorInfo.LastUpdateCheckDate = DateTime.Now;
        emulatorInfo.UpdateAvailable = false;
    }

    /// <summary>
    /// Uninstalls the shad360 emulator by removing all associated files and directories
    /// </summary>
    /// <returns>null to indicate that the emulator has been removed from settings</returns>
    public static EmulatorInfo? UninstallEmulator()
    {
        Logger.Info<XeniaService>($"Starting uninstallation process for shad360");

        string emulatorDirectory = AppPathResolver.GetFullPath(EmulatorPaths.EmulatorDir);

        // Check if the emulator directory exists before attempting deletion
        if (Directory.Exists(emulatorDirectory))
        {
            Logger.Info<XeniaService>($"Deleting shad360 emulator directory: {emulatorDirectory}");

            try
            {
                Directory.Delete(emulatorDirectory, true);
                Logger.Info<XeniaService>($"Successfully deleted shad360 emulator directory: {emulatorDirectory}");
            }
            catch (Exception ex)
            {
                Logger.Error<XeniaService>($"Failed to delete shad360 emulator directory: {ex.Message}");
                Logger.LogExceptionDetails<XeniaService>(ex);
                throw;
            }
        }
        else
        {
            Logger.Warning<XeniaService>($"shad360 emulator directory does not exist, skipping deletion: {emulatorDirectory}");
        }

        // Remove all games using shad360
        Logger.Info<XeniaService>($"Removing all games using shad360 from library");
        foreach (Game game in GameManager.Games.ToList().Where(game => game.EmulatorVersion == EmulatorVersion.Shad360))
        {
            Logger.Info<XeniaService>($"Removing game '{game.Title}' ({game.GameId}) from library since it's using shad360");
            GameManager.RemoveGame(game);
        }

        // Log the completion of the uninstallation process
        Logger.Info<XeniaService>($"Completed uninstallation process for shad360");

        // Remove the emulator from the settings by returning null
        Logger.Debug<XeniaService>($"Returning null to indicate emulator removal from settings for shad360");
        return null;
    }
}