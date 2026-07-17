using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shad360.Controls;
using Shad360.Core.Constants;
using Shad360.Core.Files;
using Shad360.Core.Logging;
using Shad360.Core.Manage;
using Shad360.Core.Models;
using Shad360.Core.Models.Files.Account;
using Shad360.Core.Services;
using Shad360.Core.Settings;
using Shad360.Core.Settings.Sections;
using Shad360.Core.Utilities;
using Shad360.Services;

namespace Shad360.ViewModels.Pages;

public partial class ManagePageViewModel : ViewModelBase
{
    private Settings _settings;
    private IMessageBoxService _messageBoxService;
    private IReleaseService _releaseService;
    private LibraryPageViewModel _libraryPageViewModel;

    // Download Progress Card
    [ObservableProperty] private int downloadProgress;
    [ObservableProperty] private bool isDownloading = false;
    [ObservableProperty] private string downloadProgressStatus = string.Empty;

    // shad360
    [ObservableProperty] private bool shad360Installed;
    [ObservableProperty] private string shad360Version = string.Empty;
    [ObservableProperty] private bool shad360Install;
    [ObservableProperty] private bool shad360Uninstall;
    [ObservableProperty] private bool shad360Update;
    [ObservableProperty] private bool shad360CheckForUpdates;

    // Emulator Settings
    [ObservableProperty] private bool isShad360Installed;
    [ObservableProperty] private bool automaticSaveBackup;
    partial void OnAutomaticSaveBackupChanged(bool value)
    {
        if (value && IsShad360Installed)
        {
            string savedProfileXuid = _settings.Settings.Emulator.Settings.Profile.ProfileXuid;

            bool shouldAutoSelect = string.IsNullOrEmpty(savedProfileXuid) ||
                                    savedProfileXuid == "0" ||
                                    (Profiles.Count > 0 && Profiles.All(p => p.DisplayXuid.ToString() != savedProfileXuid));

            if (shouldAutoSelect)
            {
                if (Profiles.Count > 0)
                {
                    SelectedProfile = Profiles.First();
                    Logger.Info<ManagePageViewModel>($"Auto-selected first profile: {SelectedProfile.Gamertag}");
                }
                else
                {
                    AutomaticSaveBackup = false;
                    _ = _messageBoxService.ShowErrorAsync(
                        LocalizationHelper.GetText("ManagePage.Content.AutomaticSaveBackup.NoProfiles.Title"),
                        LocalizationHelper.GetText("ManagePage.Content.AutomaticSaveBackup.NoProfiles.Message"));
                    return;
                }
            }
        }
    }

    [ObservableProperty] private ObservableCollection<AccountContent> profiles = [];
    [ObservableProperty] private AccountContent? selectedProfile;
    partial void OnSelectedProfileChanged(AccountContent? value)
    {
        if (value != null)
        {
            _settings.Settings.Emulator.Settings.Profile.ProfileXuid = value.DisplayXuid.ToString();
            _settings.SaveSettings();
            Logger.Info<ManagePageViewModel>($"Selected profile for automatic save backup: {value.Gamertag}");
        }
    }

    // Unified Content Folder
    [ObservableProperty] private bool unifiedContentFolder;
    partial void OnUnifiedContentFolderChanged(bool value)
    {
        if (IsShad360Installed)
        {
            if (value)
                InstallationHelper.UnifyContentFolder([EmulatorVersion.Shad360]);
            else
                InstallationHelper.SeparateContentFolder([EmulatorVersion.Shad360]);
        }
        _settings.SaveSettings();
    }

    // XConfig Settings
    [ObservableProperty] private bool xConfigEnabled;
    [ObservableProperty] private bool xConfigShow;

    // Constructor
    public ManagePageViewModel(Settings settings, IMessageBoxService messageBoxService, IReleaseService releaseService, LibraryPageViewModel libraryPageViewModel)
    {
        _settings = settings;
        _messageBoxService = messageBoxService;
        _releaseService = releaseService;
        _libraryPageViewModel = libraryPageViewModel;

        shad360Installed = _settings.IsShad360Installed(_settings);
        isShad360Installed = shad360Installed;
        shad360Version = _settings.Settings.Emulator.Shad360?.Version ?? string.Empty;

        automaticSaveBackup = _settings.Settings.Emulator.Settings.Profile.AutomaticSaveBackup;
        unifiedContentFolder = _settings.Settings.Emulator.Settings.General.UnifiedContentFolder;
        xConfigEnabled = _settings.Settings.Emulator.Settings.General.XConfigEnabled;
        xConfigShow = _settings.Settings.Emulator.Settings.General.XConfigShow;

        // Load profiles
        LoadProfiles();

        // Check for updates
        _ = CheckForUpdatesAsync();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        var profileList = ProfileManager.LoadProfiles();
        foreach (var profile in profileList)
        {
            Profiles.Add(new AccountContent(profile, EmulatorVersion.Shad360, "00000000"));
        }

        // Select the saved profile
        string savedProfileXuid = _settings.Settings.Emulator.Settings.Profile.ProfileXuid;
        if (!string.IsNullOrEmpty(savedProfileXuid) && savedProfileXuid != "0")
        {
            SelectedProfile = Profiles.FirstOrDefault(p => p.DisplayXuid.ToString() == savedProfileXuid);
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (!IsShad360Installed) return;

        try
        {
            shad360CheckForUpdates = true;
            var (isUpdateAvailable, latestVersion) = await XeniaService.CheckForUpdatesAsync(_releaseService, _settings.Settings.Emulator.Shad360!, ReleaseType.Shad360);
            if (isUpdateAvailable)
            {
                shad360Update = true;
                shad360Version = $"{_settings.Settings.Emulator.Shad360.Version} \u2192 {latestVersion}";
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>($"Failed to check for updates: {ex.Message}");
        }
        finally
        {
            shad360CheckForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task InstallShad360Async()
    {
        if (!shad360Install) return;

        try
        {
            _settings.Settings.Emulator.Shad360 = XeniaService.SetupEmulator("latest");
            _settings.SaveSettings();
            shad360Installed = true;
            isShad360Installed = true;
            shad360Version = _settings.Settings.Emulator.Shad360.Version;
            shad360Install = false;
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>($"Failed to install shad360: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Installation Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task UninstallShad360Async()
    {
        if (!shad360Uninstall) return;

        try
        {
            _settings.Settings.Emulator.Shad360 = XeniaService.UninstallEmulator();
            _settings.SaveSettings();
            shad360Installed = false;
            isShad360Installed = false;
            shad360Version = string.Empty;
            shad360Uninstall = false;
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>($"Failed to uninstall shad360: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Uninstallation Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task UpdateShad360Async()
    {
        if (!shad360Update) return;

        try
        {
            XeniaService.UpdateEmulator(_settings.Settings.Emulator.Shad360!, "latest");
            shad360Version = _settings.Settings.Emulator.Shad360!.Version;
            shad360Update = false;
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>($"Failed to update shad360: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Update Failed", ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenShad360FolderAsync()
    {
        if (!IsShad360Installed) return;

        string emulatorDir = EmulatorPaths.EmulatorDir;
        if (Directory.Exists(emulatorDir))
        {
            await ProcessUtilities.OpenFolderAsync(emulatorDir);
        }
    }

    [RelayCommand]
    private async Task OpenShad360ConfigFolderAsync()
    {
        if (!IsShad360Installed) return;

        string configDir = EmulatorPaths.ConfigFolderLocation;
        if (Directory.Exists(configDir))
        {
            await ProcessUtilities.OpenFolderAsync(configDir);
        }
    }

    [RelayCommand]
    private async Task OpenShad360PatchFolderAsync()
    {
        if (!IsShad360Installed) return;

        string patchDir = EmulatorPaths.PatchFolderLocation;
        if (Directory.Exists(patchDir))
        {
            await ProcessUtilities.OpenFolderAsync(patchDir);
        }
    }

    [RelayCommand]
    private async Task OpenShad360ContentFolderAsync()
    {
        if (!IsShad360Installed) return;

        string contentDir = EmulatorPaths.ContentFolderLocation;
        if (Directory.Exists(contentDir))
        {
            await ProcessUtilities.OpenFolderAsync(contentDir);
        }
    }
}