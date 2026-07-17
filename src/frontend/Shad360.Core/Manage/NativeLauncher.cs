using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Shad360.Core.Logging;
using Shad360.Core.Models.Game;

namespace Shad360.Core.Manage
{
    /// <summary>
    /// Native launcher that uses the embedded shad360 library (libxenia) instead of launching external processes
    /// This provides better integration, performance, and control over the emulation session
    /// </summary>
    public class NativeLauncher
    {
        private Native.Shad360Api.EmulatorHandle _emulatorHandle;
        private bool _isInitialized = false;
        private bool _isRunning = false;
        private Game? _currentGame;
        private Native.Shad360Api.StateCallback? _stateCallback;
        private Native.Shad360Api.GameEventCallback? _gameEventCallback;

        public bool IsRunning => _isRunning;
        public bool IsPaused { get; private set; } = false;
        public Game? CurrentGame => _currentGame;

        public event Action<Game>? OnGameStarted;
        public event Action<Game>? OnGameStopped;
        public event Action<string>? OnError;
        public event Action<Native.Shad360Api.Stats>? OnStatsUpdate;

        /// <summary>
        /// Initializes the native shad360 emulator
        /// </summary>
        public bool Initialize(Native.Shad360Api.Config config)
        {
            if (_isInitialized)
            {
                Logger.Warning<NativeLauncher>("Emulator already initialized");
                return true;
            }

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_init(ref config, out _emulatorHandle);
                
                if (status != Native.Shad360Api.Status.Success)
                {
                    Logger.Error<NativeLauncher>($"Failed to initialize emulator: {Native.Shad360Api.GetStatusString(status)}");
                    OnError?.Invoke($"Failed to initialize emulator: {Native.Shad360Api.GetStatusString(status)}");
                    return false;
                }

                // Set up callbacks
                _stateCallback = OnStateChanged;
                _gameEventCallback = OnGameEvent;
                
                Native.Shad360Api.shad360_set_state_callback(_stateCallback, IntPtr.Zero);
                Native.Shad360Api.shad360_set_game_event_callback(_gameEventCallback, IntPtr.Zero);

                _isInitialized = true;
                Logger.Info<NativeLauncher>("Native shad360 emulator initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception during emulator initialization: {ex.Message}");
                OnError?.Invoke($"Exception during initialization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the native emulator
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;

            try
            {
                if (_isRunning)
                {
                    StopGame();
                }

                Native.Shad360Api.Status status = Native.Shad360Api.shad360_shutdown(_emulatorHandle);
                if (status != Native.Shad360Api.Status.Success)
                {
                    Logger.Error<NativeLauncher>($"Error during shutdown: {Native.Shad360Api.GetStatusString(status)}");
                }

                _isInitialized = false;
                _emulatorHandle = default;
                Logger.Info<NativeLauncher>("Native shad360 emulator shutdown complete");
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception during shutdown: {ex.Message}");
            }
        }

        /// <summary>
        /// Launches a game using the native emulator
        /// </summary>
        public bool LaunchGame(Game game)
        {
            if (!_isInitialized)
            {
                OnError?.Invoke("Emulator not initialized");
                return false;
            }

            if (_isRunning)
            {
                OnError?.Invoke("A game is already running");
                return false;
            }

            if (!game.FileLocations.IsGamePathValid)
            {
                OnError?.Invoke($"Invalid game path: {game.FileLocations.Game}");
                return false;
            }

            try
            {
                Logger.Info<NativeLauncher>($"Launching game: {game.Title} using native shad360 backend");
                _currentGame = game;

                Native.Shad360Api.Status status = Native.Shad360Api.shad360_launch_game(_emulatorHandle, game.FileLocations.Game);
                
                if (status != Native.Shad360Api.Status.Success)
                {
                    Logger.Error<NativeLauncher>($"Failed to launch game: {Native.Shad360Api.GetStatusString(status)}");
                    OnError?.Invoke($"Failed to launch game: {Native.Shad360Api.GetStatusString(status)}");
                    _currentGame = null;
                    return false;
                }

                _isRunning = true;
                IsPaused = false;
                OnGameStarted?.Invoke(game);
                Logger.Info<NativeLauncher>($"Game launched successfully: {game.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception launching game: {ex.Message}");
                OnError?.Invoke($"Exception launching game: {ex.Message}");
                _currentGame = null;
                return false;
            }
        }

        /// <summary>
        /// Stops the currently running game
        /// </summary>
        public bool StopGame()
        {
            if (!_isInitialized || !_isRunning)
            {
                return true;
            }

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_stop_game(_emulatorHandle);
                
                if (status != Native.Shad360Api.Status.Success)
                {
                    Logger.Error<NativeLauncher>($"Failed to stop game: {Native.Shad360Api.GetStatusString(status)}");
                    return false;
                }

                _isRunning = false;
                IsPaused = false;
                Game? game = _currentGame;
                _currentGame = null;
                
                if (game != null)
                {
                    OnGameStopped?.Invoke(game);
                }
                
                Logger.Info<NativeLauncher>("Game stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception stopping game: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pauses the emulation
        /// </summary>
        public bool Pause()
        {
            if (!_isInitialized || !_isRunning || IsPaused) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_pause(_emulatorHandle);
                if (status == Native.Shad360Api.Status.Success)
                {
                    IsPaused = true;
                    Logger.Info<NativeLauncher>("Emulation paused");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception pausing: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resumes the emulation
        /// </summary>
        public bool Resume()
        {
            if (!_isInitialized || !_isRunning || !IsPaused) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_resume(_emulatorHandle);
                if (status == Native.Shad360Api.Status.Success)
                {
                    IsPaused = false;
                    Logger.Info<NativeLauncher>("Emulation resumed");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception resuming: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the emulator state to a file
        /// </summary>
        public bool SaveState(string path)
        {
            if (!_isInitialized || !_isRunning) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_save_state(_emulatorHandle, path);
                if (status == Native.Shad360Api.Status.Success)
                {
                    Logger.Info<NativeLauncher>($"State saved to: {path}");
                    return true;
                }
                Logger.Error<NativeLauncher>($"Failed to save state: {Native.Shad360Api.GetStatusString(status)}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception saving state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the emulator state from a file
        /// </summary>
        public bool LoadState(string path)
        {
            if (!_isInitialized || !_isRunning) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_load_state(_emulatorHandle, path);
                if (status == Native.Shad360Api.Status.Success)
                {
                    Logger.Info<NativeLauncher>($"State loaded from: {path}");
                    return true;
                }
                Logger.Error<NativeLauncher>($"Failed to load state: {Native.Shad360Api.GetStatusString(status)}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception loading state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets current emulator stats
        /// </summary>
        public Native.Shad360Api.Stats? GetStats()
        {
            if (!_isInitialized) return null;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_get_stats(_emulatorHandle, out Native.Shad360Api.Stats stats);
                if (status == Native.Shad360Api.Status.Success)
                {
                    return stats;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception getting stats: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets current game info
        /// </summary>
        public Native.Shad360Api.GameInfo? GetGameInfo()
        {
            if (!_isInitialized || !_isRunning) return null;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_get_game_info(_emulatorHandle, out Native.Shad360Api.GameInfo info);
                if (status == Native.Shad360Api.Status.Success)
                {
                    return info;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception getting game info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets controller state for a specific port
        /// </summary>
        public bool SetControllerState(Native.Shad360Api.ControllerPort port, Native.Shad360Api.ControllerState state)
        {
            if (!_isInitialized) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_set_controller_state(_emulatorHandle, port, ref state);
                return status == Native.Shad360Api.Status.Success;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception setting controller state: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Mounts a path in the virtual file system
        /// </summary>
        public bool MountPath(string path, string mountPoint)
        {
            if (!_isInitialized) return false;

            try
            {
                Native.Shad360Api.Status status = Native.Shad360Api.shad360_mount_path(_emulatorHandle, path, mountPoint);
                return status == Native.Shad360Api.Status.Success;
            }
            catch (Exception ex)
            {
                Logger.Error<NativeLauncher>($"Exception mounting path: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// State change callback from native code
        /// </summary>
        private void OnStateChanged(Native.Shad360Api.State state, IntPtr userData)
        {
            Logger.Debug<NativeLauncher>($"Emulator state changed: {Native.Shad360Api.GetStateString(state)}");
            
            switch (state)
            {
                case Native.Shad360Api.State.Running:
                    _isRunning = true;
                    IsPaused = false;
                    break;
                case Native.Shad360Api.State.Paused:
                    IsPaused = true;
                    break;
                case Native.Shad360Api.State.Stopping:
                    _isRunning = false;
                    IsPaused = false;
                    break;
                case Native.Shad360Api.State.Error:
                    _isRunning = false;
                    IsPaused = false;
                    OnError?.Invoke("Emulator entered error state");
                    break;
            }
        }

        /// <summary>
        /// Game event callback from native code
        /// </summary>
        private void OnGameEvent(string eventType, string data, IntPtr userData)
        {
            Logger.Debug<NativeLauncher>($"Game event: {eventType} - {data}");
        }

        /// <summary>
        /// Updates stats periodically (call from UI timer)
        /// </summary>
        public void UpdateStats()
        {
            var stats = GetStats();
            if (stats.HasValue)
            {
                OnStatsUpdate?.Invoke(stats.Value);
            }
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static Native.Shad360Api.Config CreateDefaultConfig(string? storagePath = null, string? contentPath = null, string? cachePath = null)
        {
            Native.Shad360Api.Config config = default;
            Native.Shad360Api.shad360_config_default(ref config);
            
            // Set paths if provided
            if (!string.IsNullOrEmpty(storagePath)) config.storage_path = storagePath;
            if (!string.IsNullOrEmpty(contentPath)) config.content_path = contentPath;
            if (!string.IsNullOrEmpty(cachePath)) config.cache_path = cachePath;

            // Default settings
            config.gfx_backend = Native.Shad360Api.GfxBackend.Auto;
            config.audio_backend = Native.Shad360Api.AudioBackend.Auto;
            config.enable_debug_ui = 0;
            config.enable_vsync = 1;
            config.fullscreen = 0;
            config.resolution_width = 1280;
            config.resolution_height = 720;
            config.cpu_threads = Environment.ProcessorCount;
            config.enable_jit = 1;
            config.enable_hle = 1;
            config.cpu_accuracy = 1.0f;
            config.gpu_accuracy = 1.0f;

            return config;
        }
    }
}