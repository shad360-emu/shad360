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
    public EmulatorVersion EmulatorVersion { get; init; }
    public Game Game { get; init; } = new Game();
}

public partial class XeniaSettingsPageViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ConfigFileItem> _configFiles = [];
    [ObservableProperty] private ConfigFileItem? _selectedConfigFile;
    
    partial void OnSelectedConfigFileChanged(ConfigFileItem? value)
    {
        if (value == null)
        {
            ConfigEditorViewModel = null;
            HasConfigFile = false;
            IsSelectedConfigEmulatorConfig = false;
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

    public XeniaSettingsPageViewModel()
    {
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        LoadAllConfigFiles();

        // Subscribe to game library changes to refresh the config file list
        EventManager.Instance.GameLibraryChanged += OnGameLibraryChanged;
    }

    /// <summary>
    /// Handles game library changes by refreshing the config file list.
    /// </summary>
    private void OnGameLibraryChanged()
    {
        LoadAllConfigFiles();
    }

    /// <summary>
    /// Loads all emulator and game config files into a single list.
    /// Emulator configs are loaded first, then game configs.
    /// </summary>
    private void LoadAllConfigFiles()
    {
        Logger.Info<XeniaSettingsPageViewModel>("Loading all configuration files");
        ConfigFiles.Clear();

        // First, add emulator config (if installed)
        if (EmulatorSettings.IsShad360Installed(Settings.Load()))
        {
            string configPath = AppPathResolver.GetFullPath(EmulatorPaths.ConfigLocation);
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ConfigFiles.Add(new ConfigFileItem
                {
                    DisplayName = "shad360 Emulator Config",
                    FilePath = configPath,
                    IsEmulatorConfig = true,
                    EmulatorVersion = EmulatorVersion.Shad360
                });
                Logger.Debug<XeniaSettingsPageViewModel>($"Added emulator config: {configPath}");
            }
        }

        // Then, add game configs
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
                    EmulatorVersion = game.EmulatorVersion,
                    Game = game
                });
                Logger.Debug<XeniaSettingsPageViewModel>($"Added game config: {configPath}");
            }
        }

        Logger.Info<XeniaSettingsPageViewModel>($"Loaded {ConfigFiles.Count} configuration files");
    }

    /// <summary>
    /// Loads a configuration file into the editor.
    /// </summary>
    private async void LoadConfigFile(ConfigFileItem configFile)
    {
        try
        {
            CurrentConfigFilePath = configFile.FilePath;
            HasConfigFile = true;

            ConfigDocument configDocument = ConfigFile.Load(configFile.FilePath);
            ConfigEditorViewModel = new ConfigEditorViewModel(configDocument, configFile.IsEmulatorConfig, configFile.Game);
            Logger.Info<XeniaSettingsPageViewModel>($"Loaded config file: {configFile.DisplayName}");
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to load config file {configFile.FilePath}: {ex.Message}");
            HasConfigFile = false;
            await _messageBoxService.ShowErrorAsync("Error Loading Config", $"Failed to load configuration file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SaveConfigFileAsync()
    {
        if (ConfigEditorViewModel == null || string.IsNullOrEmpty(CurrentConfigFilePath))
        {
            return;
        }

        try
        {
            ConfigEditorViewModel.SaveConfig(CurrentConfigFilePath);
            Logger.Info<XeniaSettingsPageViewModel>($"Saved config file: {CurrentConfigFilePath}");
            await _messageBoxService.ShowInformationAsync("Success", "Configuration saved successfully!");
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to save config file: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Error Saving Config", $"Failed to save configuration file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CreateGameConfigAsync()
    {
        // Create a game-specific config based on the emulator config
        if (!EmulatorSettings.IsShad360Installed(Settings.Load()))
        {
            await _messageBoxService.ShowErrorAsync("Not Installed", "shad360 is not installed. Please install it first from the Manage page.");
            return;
        }

        string emulatorConfigPath = AppPathResolver.GetFullPath(EmulatorPaths.DefaultConfigLocation);
        if (!File.Exists(emulatorConfigPath))
        {
            await _messageBoxService.ShowErrorAsync("Config Not Found", "Default emulator config not found.");
            return;
        }

        // Ask user to select a game
        var gameSelector = new GameSelectionDialog();
        Game? selectedGame = await gameSelector.ShowAsync();
        
        if (selectedGame == null) return;

        string newConfigPath = Path.Combine(EmulatorPaths.ConfigFolderLocation, $"{selectedGame.Title}.config.toml");
        
        try
        {
            File.Copy(emulatorConfigPath, newConfigPath, true);
            selectedGame.FileLocations.Config = newConfigPath;
            GameManager.SaveLibrary();
            
            // Reload config files
            LoadAllConfigFiles();
            
            // Select the new config
            var newConfigItem = ConfigFiles.FirstOrDefault(c => c.FilePath == newConfigPath);
            if (newConfigItem != null)
            {
                SelectedConfigFile = newConfigItem;
            }
            
            await _messageBoxService.ShowInformationAsync("Success", $"Game config created for {selectedGame.Title}");
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to create game config: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Error", $"Failed to create game config: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OptimizeSettingsAsync()
    {
        if (SelectedConfigFile == null || SelectedConfigFile.IsEmulatorConfig || SelectedConfigFile.Game == null)
        {
            await _messageBoxService.ShowErrorAsync("Invalid Selection", "Please select a game configuration file to optimize.");
            return;
        }

        try
        {
            await OptimizedSettingsDatabase.ApplyOptimizedSettingsAsync(SelectedConfigFile.Game, SelectedConfigFile.FilePath);
            Logger.Info<XeniaSettingsPageViewModel>($"Applied optimized settings for {SelectedConfigFile.Game.Title}");
            await _messageBoxService.ShowInformationAsync("Success", "Optimized settings applied successfully!");
            
            // Reload the config
            LoadConfigFile(SelectedConfigFile);
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to apply optimized settings: {ex.Message}");
            await _messageBoxService.ShowErrorAsync("Error", $"Failed to apply optimized settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenConfigFolderAsync()
    {
        if (!EmulatorSettings.IsShad360Installed(Settings.Load()))
        {
            return;
        }

        string configDir = EmulatorPaths.ConfigFolderLocation;
        if (Directory.Exists(configDir))
        {
            await ProcessUtilities.OpenFolderAsync(configDir);
        }
    }
}