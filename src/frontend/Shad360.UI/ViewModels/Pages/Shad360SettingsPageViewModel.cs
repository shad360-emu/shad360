using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shad360.Core.Files;
using Shad360.Core.Logging;
using Shad360.Core.Manage;
using Shad360.Core.Models;
using Shad360.Core.Models.Files.Config;
using Shad360.Core.Models.Game;
using Shad360.Core.Services;
using Shad360.Core.Utilities;
using Shad360.Services;
using Shad360.ViewModels.Controls;
using Shad360.Controls;
using Shad360.Core.Database;

namespace Shad360.ViewModels.Pages;

/// <summary>
/// Represents a configuration file item in the settings page.
/// </summary>
public partial class ConfigFileItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public bool IsEmulatorConfig { get; init; }
    public Game Game { get; init; } = new Game();
}

public partial class Shad360SettingsPageViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ConfigFileItem> _configFiles = [];
    [ObservableProperty] private ConfigFileItem? _selectedConfigFile;
    
    partial void OnSelectedConfigFileChanged(ConfigFileItem? value)
    {
        if (value == null)
        {
            ConfigEditorViewModel = null;
            HasConfigFile = false;
            CanOptimizeSettings = false;
            return;
        }

        IsSelectedConfigEmulatorConfig = value.IsEmulatorConfig;
        CanOptimizeSettings = !value.IsEmulatorConfig;
        LoadConfigFile(value);
    }
    
    [ObservableProperty] private ConfigEditorViewModel? _configEditorViewModel;
    [ObservableProperty] private bool _hasConfigFile;
    [ObservableProperty] private bool _isSelectedConfigEmulatorConfig;
    [ObservableProperty] private bool _canOptimizeSettings;
    [ObservableProperty] private string _currentConfigFilePath = string.Empty;

    partial void OnHasConfigFileChanged(bool value)
    {
        if (SelectedConfigFile != null)
        {
            CanOptimizeSettings = value && !SelectedConfigFile.IsEmulatorConfig;
        }
    }

    private readonly IMessageBoxService _messageBoxService;

    public Shad360SettingsPageViewModel()
    {
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        LoadAllConfigFiles();

        EventManager.Instance.GameLibraryChanged += OnGameLibraryChanged;
    }

    private void OnGameLibraryChanged()
    {
        LoadAllConfigFiles();
    }

    private void LoadAllConfigFiles()
    {
        Logger.Info<Shad360SettingsPageViewModel>("Loading all configuration files");
        ConfigFiles.Clear();

        // Add shad360 emulator config
        try
        {
            string configPath = AppPathResolver.GetFullPath(EmulatorPaths.ConfigLocation);
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ConfigFiles.Add(new ConfigFileItem
                {
                    DisplayName = "shad360",
                    FilePath = configPath,
                    IsEmulatorConfig = true
                });
                Logger.Debug<Shad360SettingsPageViewModel>($"Added emulator config: {configPath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<Shad360SettingsPageViewModel>($"Failed to load emulator config");
            Logger.LogExceptionDetails<Shad360SettingsPageViewModel>(ex);
        }

        // Add game configs
        foreach (Game game in GameManager.Games)
        {
            string configPath = AppPathResolver.GetFullPath(game.FileLocations.Config);
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ConfigFiles.Add(new ConfigFileItem
                {
                    DisplayName = game.Title,
                    FilePath = configPath,
                    IsEmulatorConfig = false,
                    Game = game
                });
                Logger.Debug<Shad360SettingsPageViewModel>($"Added game config: {configPath}");
            }
        }
    }

    private void LoadConfigFile(ConfigFileItem item)
    {
        try
        {
            if (item.IsEmulatorConfig)
            {
                ConfigEditorViewModel = new ConfigEditorViewModel(item.FilePath, null);
            }
            else
            {
                ConfigEditorViewModel = new ConfigEditorViewModel(item.FilePath, item.Game);
            }
            HasConfigFile = true;
            CurrentConfigFilePath = item.FilePath;
        }
        catch (Exception ex)
        {
            Logger.Error<Shad360SettingsPageViewModel>($"Failed to load config file: {ex.Message}");
            _ = _messageBoxService.ShowErrorAsync("Error", $"Failed to load config file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveConfigFileAsync()
    {
        if (ConfigEditorViewModel == null || !HasConfigFile) return;

        try
        {
            ConfigManager.SaveConfigurationFile(CurrentConfigFilePath, EmulatorVersion.Shad360);
            await _messageBoxService.ShowInformationAsync("Success", "Configuration saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<Shad360SettingsPageViewModel>($"Failed to save config: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Error", $"Failed to save config: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ResetConfigFileAsync()
    {
        if (!HasConfigFile || SelectedConfigFile == null) return;

        bool confirm = await _messageBoxService.ShowConfirmAsync(
            "Reset Configuration",
            $"Are you sure you want to reset '{SelectedConfigFile.DisplayName}' configuration to defaults?");

        if (!confirm) return;

        try
        {
            string configPath = AppPathResolver.GetFullPath(EmulatorPaths.ConfigLocation);
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
            ConfigManager.GenerateEmulatorConfigurationFile(EmulatorVersion.Shad360, true);
            LoadAllConfigFiles();
            await _messageBoxService.ShowInformationAsync("Success", "Configuration reset to defaults");
        }
        catch (Exception ex)
        {
            Logger.Error<Shad360SettingsPageViewModel>($"Failed to reset config: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Error", $"Failed to reset config: {ex.Message}");
        }
    }
}