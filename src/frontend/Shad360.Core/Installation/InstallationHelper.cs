using Microsoft.Win32;
using Shad360.Core.Constants;
using Shad360.Core.Logging;
using Shad360.Core.Models;
using Shad360.Core.Utilities;

namespace Shad360.Core.Installation;

/// <summary>
/// Provides utility methods for installing and configuring shad360 emulator
/// </summary>
public class InstallationHelper
{
    /// <summary>
    /// Registry path where shad360 stores its configuration flags
    /// </summary>
    private const string REGISTRY_PATH = @"Software\shad360";

    /// <summary>
    /// Name of the registry value used to suppress first-launch popups
    /// </summary>
    private const string REGISTRY_VALUE_NAME = "XEFLAGS";

    /// <summary>
    /// Value to set in the registry to suppress first-launch popups
    /// </summary>
    private const long REGISTRY_VALUE_DATA = 1;

    /// <summary>
    /// Sets up the Windows registry key to suppress the first-launch popup in shad360
    /// This method creates or updates the XEFLAGS registry value to prevent shad360 from showing
    /// its initial configuration dialog on the first launch
    /// </summary>
    /// <remarks>
    /// This method only executes on Windows systems. On other platforms, it returns immediately.
    /// The registry key is created under HKEY_CURRENT_USER\Software\shad360
    /// </remarks>
    public static void RegistrySetup()
    {
        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<InstallationHelper>("Skipping registry setup on non-Windows platform");
            return;
        }

        // Attempt to open the registry key for writing or create it if it doesn't exist
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, writable: true)
                                ?? Registry.CurrentUser.CreateSubKey(REGISTRY_PATH, writable: true);

        if (key.GetValue(REGISTRY_VALUE_NAME) == null)
        {
            // Set the registry value to suppress the first-launch popup
            key.SetValue(REGISTRY_VALUE_NAME, REGISTRY_VALUE_DATA, RegistryValueKind.QWord);
            Logger.Info<InstallationHelper>("XEFLAGS registry value created successfully.");
        }
        else
        {
            Logger.Debug<InstallationHelper>("XEFLAGS registry value already exists.");
        }
    }

    /// <summary>
    /// Configures the emulator directory for portable mode and creates the necessary subdirectories
    /// This method ensures shad360 runs in portable mode by creating a portable.txt file
    /// and establishes the required directory structure for optimal operation
    /// </summary>
    /// <param name="emulatorLocation">The directory path where the shad360 executable is located</param>
    /// <remarks>
    /// Creates the following directory structure:
    /// - portable.txt file (enables portable mode)
    /// - config/ directory (for game-specific configurations)
    /// - content/ directory (for game content storage)
    /// - patches/ directory (for game patches)
    /// - shaders/ directory (for shader cache)
    /// </remarks>
    public static void SetupEmulatorDirectory(string emulatorLocation)
    {
        Logger.Info<InstallationHelper>($"Setting up emulator directory at: {emulatorLocation}");

        // Create portable.txt to enable portable mode for shad360
        string portableTxtPath = Path.Combine(emulatorLocation, "portable.txt");
        if (!File.Exists(portableTxtPath))
        {
            File.Create(portableTxtPath).Dispose();
            Logger.Info<InstallationHelper>($"Created portable.txt file at: {portableTxtPath}");
        }
        else
        {
            Logger.Debug<InstallationHelper>($"Portable.txt file already exists at: {portableTxtPath}");
        }

        // Create the config directory for storing game-specific configuration files
        string configDirPath = Path.Combine(emulatorLocation, "config");
        Directory.CreateDirectory(configDirPath);
        Logger.Info<InstallationHelper>($"Ensured config directory exists at: {configDirPath}");

        // Create the content directory for storing game-specific content files
        string contentDirPath = Path.Combine(emulatorLocation, "content");
        Directory.CreateDirectory(contentDirPath);
        Logger.Info<InstallationHelper>($"Ensured content directory exists at: {contentDirPath}");

        // Create the patches directory for storing game patches
        string patchesDirPath = Path.Combine(emulatorLocation, "patches");
        Directory.CreateDirectory(patchesDirPath);
        Logger.Info<InstallationHelper>($"Ensured patches directory exists at: {patchesDirPath}");

        // Create the shaders directory for shader cache
        string shadersDirPath = Path.Combine(emulatorLocation, "shaders");
        Directory.CreateDirectory(shadersDirPath);
        Logger.Info<InstallationHelper>($"Ensured shaders directory exists at: {shadersDirPath}");

        Logger.Info<InstallationHelper>($"Emulator directory setup completed at: {emulatorLocation}");
    }

    /// <summary>
    /// Unifies content folders across emulator by copying content from the main version
    /// to a centralized location and setting up symbolic links.
    /// </summary>
    /// <param name="installedVersions">A list of installed emulator versions that will share the unified content.</param>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Retrieves the content folder location of the main shad360 version
    /// 2. Deletes any existing centralized content directory
    /// 3. Copies all content from the main version to the centralized location
    /// 4. Sets up symbolic links for all other installed versions to point to the centralized content
    /// </remarks>
    public static void UnifyContentFolder(List<EmulatorVersion> installedVersions)
    {
        Logger.Info<InstallationHelper>($"Starting content folder unification. Installed versions: {string.Join(", ", installedVersions)}");

        string mainContentFolder = AppPathResolver.GetFullPath(EmulatorPaths.ContentFolderLocation);
        Logger.Debug<InstallationHelper>($"Main shad360 content folder: {mainContentFolder}");

        if (Directory.Exists(AppPaths.EmulatorsContentDirectory))
        {
            Logger.Debug<InstallationHelper>($"Existing centralized content directory found at: {AppPaths.EmulatorsContentDirectory}. Deleting...");
            Directory.Delete(AppPaths.EmulatorsContentDirectory, true);
            Logger.Info<InstallationHelper>($"Deleted existing centralized content directory");
        }

        Logger.Info<InstallationHelper>($"Copying content from main version to centralized directory at: {AppPaths.EmulatorsContentDirectory}");
        StorageUtilities.CopyDirectory(mainContentFolder, AppPaths.EmulatorsContentDirectory, true);
        Logger.Info<InstallationHelper>($"Successfully copied content from main version to centralized directory");

        foreach (EmulatorVersion version in installedVersions)
        {
            Logger.Debug<InstallationHelper>($"Setting up content folder for version: {version}");
            string versionContentFolder = AppPathResolver.GetFullPath(EmulatorPaths.ContentFolderLocation);
            SetupContentFolder(versionContentFolder);
            Logger.Info<InstallationHelper>($"Content folder setup completed for version: {version}");
        }

        Logger.Info<InstallationHelper>("Content folder unification completed successfully");
    }

    /// <summary>
    /// Separates the unified content folder by copying content from the centralized location
    /// back to each emulator version's content folder.
    /// This method reverses the unification process, giving each emulator version its own independent content directory.
    /// </summary>
    /// <param name="installedVersions">A list of installed emulator versions that will receive their own content folders.</param>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Iterates through each installed emulator version
    /// 2. Deletes the existing content folder for each version
    /// 3. Copies all content from the centralized directory to each version's individual content folder
    /// </remarks>
    public static void SeparateContentFolder(List<EmulatorVersion> installedVersions)
    {
        Logger.Info<InstallationHelper>($"Starting content folder separation for {installedVersions.Count} version(s): {string.Join(", ", installedVersions)}");

        foreach (EmulatorVersion version in installedVersions)
        {
            Logger.Debug<InstallationHelper>($"Processing version: {version}");
            string versionContentFolder = AppPathResolver.GetFullPath(EmulatorPaths.ContentFolderLocation);
            Logger.Debug<InstallationHelper>($"Version content folder path: {versionContentFolder}");

            if (Directory.Exists(versionContentFolder))
            {
                Logger.Debug<InstallationHelper>($"Existing content folder found at: {versionContentFolder}. Deleting...");
                Directory.Delete(versionContentFolder, true);
                Logger.Info<InstallationHelper>($"Deleted existing content folder for version: {version}");
            }

            if (Directory.Exists(AppPaths.EmulatorsContentDirectory))
            {
                Logger.Info<InstallationHelper>($"Copying content from centralized directory to {versionContentFolder}");
                StorageUtilities.CopyDirectory(AppPaths.EmulatorsContentDirectory, versionContentFolder, true);
                Logger.Info<InstallationHelper>($"Content folder separation completed for version: {version}");
            }
            else
            {
                Logger.Warning<InstallationHelper>($"Centralized content directory does not exist. Skipping content copy for version: {version}");
            }
        }

        Logger.Info<InstallationHelper>("Content folder separation completed successfully");
    }

    /// <summary>
    /// Sets up a symbolic link for the emulator content folder to a centralized location.
    /// This method ensures that all emulator versions share the same content directory,
    /// allowing games and saves to be accessible across different emulator versions.
    /// </summary>
    /// <param name="emulatorContentFolder">The path to the emulator's content folder that will be replaced with a symbolic link.</param>
    /// <exception cref="Exception">Thrown when the symbolic link creation or verification fails.</exception>
    public static void SetupContentFolder(string emulatorContentFolder)
    {
        Logger.Trace<InstallationHelper>($"Starting SetupContentFolder operation for: {emulatorContentFolder}");

        Logger.Info<InstallationHelper>($"Creating centralized content directory at: {AppPaths.EmulatorsContentDirectory}");
        Directory.CreateDirectory(AppPaths.EmulatorsContentDirectory);

        if (Directory.Exists(emulatorContentFolder))
        {
            Logger.Debug<InstallationHelper>($"Existing content folder found at: {emulatorContentFolder}. Deleting before creating symbolic link.");
            Directory.Delete(emulatorContentFolder, true);
        }

        Logger.Info<InstallationHelper>($"Creating symbolic link from {emulatorContentFolder} to {AppPaths.EmulatorsContentDirectory}");
        Directory.CreateSymbolicLink(emulatorContentFolder, AppPaths.EmulatorsContentDirectory);

        DirectoryInfo linkInfo = new DirectoryInfo(emulatorContentFolder);
        if ((linkInfo.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            Logger.Info<InstallationHelper>("Verified: Symbolic link created successfully.");
        }
        else
        {
            Logger.Error<InstallationHelper>("Failed to verify symbolic link.");
            throw new Exception("Failed to create symbolic link for content folder");
        }

        Logger.Trace<InstallationHelper>("SetupContentFolder operation completed successfully");
    }
}