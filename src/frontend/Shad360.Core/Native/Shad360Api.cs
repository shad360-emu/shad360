using System;
using System.Runtime.InteropServices;

namespace Shad360.Native
{
    /// <summary>
    /// P/Invoke wrapper for the shad360 C API
    /// This provides a managed interface to the native shad360 library (libxenia)
    /// </summary>
    public static partial class Shad360Api
    {
        private const string LibraryName = "shad360";

        // Status codes
        public enum Status : int
        {
            Success = 0,
            Error = -1,
            InvalidArgument = -2,
            NotInitialized = -3,
            AlreadyInitialized = -4,
            FileNotFound = -5,
            OutOfMemory = -6,
            GameAlreadyRunning = -7,
            NoGameRunning = -8,
            UnsupportedFile = -9,
            EmulatorError = -10,
        }

        // Graphics backend
        public enum GfxBackend : int
        {
            Auto = 0,
            Vulkan = 1,
            D3D12 = 2,
            Null = 3,
        }

        // Audio backend
        public enum AudioBackend : int
        {
            Auto = 0,
            XAudio2 = 1,
            SDL = 2,
            Null = 3,
        }

        // Emulator state
        public enum State : int
        {
            Uninitialized = 0,
            Ready = 1,
            Running = 2,
            Paused = 3,
            Stopping = 4,
            Error = 5,
        }

        // Controller port
        public enum ControllerPort : int
        {
            Port1 = 0,
            Port2 = 1,
            Port3 = 2,
            Port4 = 3,
        }

        // Configuration structure
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct Config
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string storage_path;
            [MarshalAs(UnmanagedType.LPStr)]
            public string content_path;
            [MarshalAs(UnmanagedType.LPStr)]
            public string cache_path;
            [MarshalAs(UnmanagedType.LPStr)]
            public string game_path;
            public GfxBackend gfx_backend;
            public AudioBackend audio_backend;
            public int enable_debug_ui;
            public int enable_vsync;
            public int fullscreen;
            public int resolution_width;
            public int resolution_height;
            public int cpu_threads;
            public int enable_jit;
            public int enable_hle;
            public float cpu_accuracy;
            public float gpu_accuracy;
        }

        // Game info structure
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct GameInfo
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title_name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string title_id;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string region;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string developer;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string publisher;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string genre;
            public uint title_id_hex;
            public int is_demo;
            public int is_xbla;
            public int is_xbox360;
        }

        // Emulator stats structure
        [StructLayout(LayoutKind.Sequential)]
        public struct Stats
        {
            public ulong frame_count;
            public double fps;
            public double frame_time_ms;
            public ulong cpu_cycles;
            public ulong gpu_draw_calls;
            public ulong gpu_triangles;
            public ulong memory_used_mb;
            public ulong audio_buffer_underruns;
        }

        // Controller state structure
        [StructLayout(LayoutKind.Sequential)]
        public struct ControllerState
        {
            public ushort buttons;
            public short left_trigger;
            public short right_trigger;
            public short left_thumb_x;
            public short left_thumb_y;
            public short right_thumb_x;
            public short right_thumb_y;
            public int connected;
        }

        // Opaque handle
        public struct EmulatorHandle
        {
            public IntPtr ptr;
        }

        // Callback delegates
        public delegate void LogCallback(int level, string message, IntPtr user_data);
        public delegate void StateCallback(State state, IntPtr user_data);
        public delegate void GameEventCallback(string event_type, string data, IntPtr user_data);
        public delegate void FrameCallback(IntPtr frame_data, int width, int height, int stride, IntPtr user_data);

        // Initialize emulator
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_init(ref Config config, out EmulatorHandle emulator);

        // Shutdown emulator
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_shutdown(EmulatorHandle emulator);

        // Get emulator state
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern State shad360_get_state(EmulatorHandle emulator);

        // Launch game
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_launch_game(EmulatorHandle emulator, string game_path);

        // Stop game
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_stop_game(EmulatorHandle emulator);

        // Pause/resume
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_pause(EmulatorHandle emulator);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_resume(EmulatorHandle emulator);

        // Step frame
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_step_frame(EmulatorHandle emulator);

        // Get game info
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_get_game_info(EmulatorHandle emulator, out GameInfo info);

        // Get stats
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_get_stats(EmulatorHandle emulator, out Stats stats);

        // Save/load state
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_save_state(EmulatorHandle emulator, string path);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_load_state(EmulatorHandle emulator, string path);

        // Set callbacks
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void shad360_set_log_callback(LogCallback callback, IntPtr user_data);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void shad360_set_state_callback(StateCallback callback, IntPtr user_data);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void shad360_set_game_event_callback(GameEventCallback callback, IntPtr user_data);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void shad360_set_frame_callback(FrameCallback callback, IntPtr user_data);

        // Config helpers
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void shad360_config_default(ref Config config);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr shad360_status_string(Status status);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr shad360_state_string(State state);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr shad360_version_string();

        // Get available backends
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr shad360_get_available_gfx_backends(out int count);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr shad360_get_available_audio_backends(out int count);

        // Controller
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_set_controller_state(EmulatorHandle emulator, ControllerPort port, ref ControllerState state);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_get_controller_state(EmulatorHandle emulator, ControllerPort port, out ControllerState state);

        // Memory
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_read_memory(EmulatorHandle emulator, ulong address, IntPtr buffer, nuint size);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_write_memory(EmulatorHandle emulator, ulong address, IntPtr buffer, nuint size);

        // Debug
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_debug_break(EmulatorHandle emulator);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_debug_continue(EmulatorHandle emulator);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_debug_step(EmulatorHandle emulator);

        // Module loading
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_load_module(EmulatorHandle emulator, string path);

        // Shader cache
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Status shad360_clear_shader_cache(EmulatorHandle emulator);

        // Mount path
        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern Status shad360_mount_path(EmulatorHandle emulator, string path, string mount_point);

        // Helper methods
        public static string GetStatusString(Status status)
        {
            return Marshal.PtrToStringAnsi(shad360_status_string(status)) ?? "Unknown";
        }

        public static string GetStateString(State state)
        {
            return Marshal.PtrToStringAnsi(shad360_state_string(state)) ?? "Unknown";
        }

        public static string GetVersionString()
        {
            return Marshal.PtrToStringAnsi(shad360_version_string()) ?? "Unknown";
        }

        public static string[] GetAvailableGfxBackends()
        {
            IntPtr ptr = shad360_get_available_gfx_backends(out int count);
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<string>();

            string[] result = new string[count];
            IntPtr current = ptr;
            for (int i = 0; i < count; i++)
            {
                IntPtr strPtr = Marshal.ReadIntPtr(current);
                result[i] = Marshal.PtrToStringAnsi(strPtr) ?? "";
                current = IntPtr.Add(current, IntPtr.Size);
            }
            return result;
        }

        public static string[] GetAvailableAudioBackends()
        {
            IntPtr ptr = shad360_get_available_audio_backends(out int count);
            if (ptr == IntPtr.Zero || count <= 0) return Array.Empty<string>();

            string[] result = new string[count];
            IntPtr current = ptr;
            for (int i = 0; i < count; i++)
            {
                IntPtr strPtr = Marshal.ReadIntPtr(current);
                result[i] = Marshal.PtrToStringAnsi(strPtr) ?? "";
                current = IntPtr.Add(current, IntPtr.Size);
            }
            return result;
        }
    }
}