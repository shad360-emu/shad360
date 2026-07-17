using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Shad360.Core.Logging;
using Shad360.Core.Models.Game;
using Shad360.Core.Settings;
using Shad360.Core.Utilities;

namespace Shad360.Core.Manage
{
    /// <summary>
    /// Unified launcher that uses native shad360 backend when available, 
    /// falls back to external Xenia process if needed
    /// </summary>
    public static class UnifiedLauncher
    {
        public static bool XeniaUpdating = false;
        public static NativeLauncher? NativeLauncher { get; private set; }
        public static bool UseNativeBackend { get; set; } = true;
        public static bool NativeBackendAvailable { get; private set; } = false;

        /// <summary>
        /// Initializes the native backend
        /// </summary>
        public static async Task<bool> InitializeNativeBackendAsync()
        {
            if (!UseNativeBackend)
            {
                Logger.Info<Launcher>("Native backend disabled by user preference");
                NativeBackendAvailable = false;
                return false;
            }

            try
            {
                NativeLauncher = new NativeLauncher();
                
                // Create default config
                Native.Shad360Api.Config config = NativeLauncher.CreateDefaultConfig(
                    storagePath: AppPathResolver.GetFullPath(AppPaths.EmulatorsDirectory + "/shad360/storage"),
                    contentPath: AppPathResolver.GetFullPath(AppPaths.EmulatorsDirectory + "/shad360/content"),
                    cachePath: AppPathResolver.GetFullPath(AppPaths.CacheDirectory)
                );

                bool success = NativeLauncher.Initialize(config);
                NativeBackendAvailable = success;

                if (success)
                {
                    Logger.Info<Launcher>("Native shad360 backend initialized successfully");
                    
                    // Subscribe to events
                    NativeLauncher.OnGameStarted += OnNativeGameStarted;
                    NativeLauncher.OnGameStopped += OnNativeGameStopped;
                    NativeLauncher.OnError += OnNativeError;
                    NativeLauncher.OnStatsUpdate += OnNativeStatsUpdate;
                }
                else
                {
                    Logger.Warning<Launcher>("Native backend initialization failed, will use external Xenia");
                    NativeLauncher = null;
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error<Launcher>($"Failed to initialize native backend: {ex.Message}");
                NativeBackendAvailable = false;
                NativeLauncher = null;
                return false;
            }
        }

        /// <summary>
        /// Shuts down the native backend
        /// </summary>
        public static void ShutdownNativeBackend()
        {
            if (NativeLauncher != null)
            {
                NativeLauncher.OnGameStarted -= OnNativeGameStarted;
                NativeLauncher.OnGameStopped -= OnNativeGameStopped;
                NativeLauncher.OnError -= OnNativeError;
                NativeLauncher.OnStatsUpdate -= OnNativeStatsUpdate;
                
                NativeLauncher.Shutdown();
                NativeLauncher = null;
                NativeBackendAvailable = false;
                Logger.Info<Launcher>("Native shad360 backend shutdown complete");
            }
        }

        /// <summary>
        /// Launches a game using the best available backend
        /// </summary>
        public static async Task LaunchGameAsync(Game game, Settings settings, XeniaOutputHandler? outputHandler = null, Action? onGameLoadingStarted = null, string? configOverridesFromArgs = null)
        {
            if (XeniaUpdating)
            {
                throw new Exception("shad360 is currently updating, please wait for it to finish");
            }

            // Try native backend first
            if (UseNativeBackend && NativeBackendAvailable && NativeLauncher != null)
            {
                try
                {
                    Logger.Info<Launcher>($"Attempting to launch '{game.Title}' using native shad360 backend");
                    
                    bool success = NativeLauncher.LaunchGame(game);
                    
                    if (success)
                    {
                        // Start stats update timer
                        _ = Task.Run(async () =>
                        {
                            while (NativeLauncher?.IsRunning == true)
                            {
                                NativeLauncher.UpdateStats();
                                await Task.Delay(1000);
                            }
                        });

                        // Fire the game loading started callback
                        onGameLoadingStarted?.Invoke();
                        
                        // Wait for game to finish
                        while (NativeLauncher.IsRunning)
                        {
                            await Task.Delay(500);
                        }
                        
                        Logger.Info<Launcher>($"Game session ended for '{game.Title}'");
                        return;
                    }
                    else
                    {
                        Logger.Warning<Launcher>("Native backend launch failed, falling back to external Xenia");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error<Launcher>($"Native backend error: {ex.Message}, falling back to external Xenia");
                }
            }

            // Fallback to external Xenia process
            await LaunchExternalXeniaAsync(game, settings, outputHandler, onGameLoadingStarted, configOverridesFromArgs);
        }

        /// <summary>
        /// Launches game using external Xenia process (legacy method)
        /// </summary>
        private static async Task LaunchExternalXeniaAsync(Game game, Settings settings, XeniaOutputHandler? outputHandler = null, Action? onGameLoadingStarted = null, string? configOverridesFromArgs = null)
        {
            await Launcher.LaunchGameCoreAsync(game, async: true, settings.Settings, outputHandler, onGameLoadingStarted, configOverridesFromArgs);
        }

        /// <summary>
        /// Stops the currently running game
        /// </summary>
        public static void StopGame()
        {
            if (NativeBackendAvailable && NativeLauncher?.IsRunning == true)
            {
                NativeLauncher.StopGame();
            }
            else
            {
                // External process stopping would need process tracking
                Logger.Warning<Launcher>("Cannot stop external Xenia process - not tracked");
            }
        }

        /// <summary>
        /// Pauses emulation
        /// </summary>
        public static bool Pause()
        {
            if (NativeBackendAvailable && NativeLauncher?.IsRunning == true)
            {
                return NativeLauncher.Pause();
            }
            return false;
        }

        /// <summary>
        /// Resumes emulation
        /// </summary>
        public static bool Resume()
        {
            if (NativeBackendAvailable && NativeLauncher?.IsRunning == true)
            {
                return NativeLauncher.Resume();
            }
            return false;
        }

        /// <summary>
        /// Saves emulator state
        /// </summary>
        public static bool SaveState(string path)
        {
            if (NativeBackendAvailable && NativeLauncher?.IsRunning == true)
            {
                return NativeLauncher.SaveState(path);
            }
            return false;
        }

        /// <summary>
        /// Loads emulator state
        /// </summary>
        public static bool LoadState(string path)
        {
            if (NativeBackendAvailable && NativeLauncher?.IsRunning == true)
            {
                return NativeLauncher.LoadState(path);
            }
            return false;
        }

        /// <summary>
        /// Gets current emulator stats
        /// </summary>
        public static Native.Shad360Api.Stats? GetStats()
        {
            if (NativeBackendAvailable && NativeLauncher != null)
            {
                return NativeLauncher.GetStats();
            }
            return null;
        }

        /// <summary>
        /// Checks if a game is currently running
        /// </summary>
        public static bool IsGameRunning()
        {
            return NativeBackendAvailable && NativeLauncher?.IsRunning == true;
        }

        /// <summary>
        /// Checks if emulation is paused
        /// </summary>
        public static bool IsPaused()
        {
            return NativeBackendAvailable && NativeLauncher?.IsPaused == true;
        }

        /// <summary>
        /// Gets the currently running game
        /// </summary>
        public static Game? GetCurrentGame()
        {
            return NativeLauncher?.CurrentGame;
        }

        // Event handlers for native launcher
        private static void OnNativeGameStarted(Game game)
        {
            Logger.Info<Launcher>($"Native backend: Game started - {game.Title}");
        }

        private static void OnNativeGameStopped(Game game)
        {
            Logger.Info<Launcher>($"Native backend: Game stopped - {game.Title}");
        }

        private static void OnNativeError(string error)
        {
            Logger.Error<Launcher>($"Native backend error: {error}");
        }

        private static void OnNativeStatsUpdate(Native.Shad360Api.Stats stats)
        {
            // Could update UI with real-time stats
            // Logger.Trace<Launcher>($"FPS: {stats.fps:F1}, Frame: {stats.frame_count}");
        }
    }
}