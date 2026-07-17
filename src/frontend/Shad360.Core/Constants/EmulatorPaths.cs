using Shad360.Core.Utilities;

namespace Shad360.Core.Constants;

/// <summary>
/// Static paths for the shad360 emulator (single version, no multiple Xenia versions)
/// </summary>
public static class EmulatorPaths
{
    public static string EmulatorDir => AppPathResolver.GetFullPath("Emulators/shad360");
    public static string ExecutableLocation => Path.Combine(EmulatorDir, "shad360.exe");
    public static string ConfigFolderLocation => Path.Combine(EmulatorDir, "config");
    public static string PatchFolderLocation => Path.Combine(EmulatorDir, "patches");
    public static string ScreenshotsFolderLocation => Path.Combine(EmulatorDir, "screenshots");
    public static string LogLocation => Path.Combine(EmulatorDir, "shad360.log");
    public static string ConfigLocation => Path.Combine(ConfigFolderLocation, "config.toml");
    public static string DefaultConfigLocation => Path.Combine(EmulatorDir, "config.toml");
    public static string ContentFolderLocation => Path.Combine(EmulatorDir, "content");
    public static string ShadersFolderLocation => Path.Combine(EmulatorDir, "shaders");
    public static string BindingsLocation => Path.Combine(ConfigFolderLocation, "bindings.toml");
    public static string XConfigLocation => Path.Combine(EmulatorDir, "xconfig.settings");
    
    public static string NativeLibraryLocation => OperatingSystem.IsWindows() 
        ? Path.Combine(EmulatorDir, "shad360.dll")
        : OperatingSystem.IsMacOS() 
            ? Path.Combine(EmulatorDir, "libshad360.dylib")
            : Path.Combine(EmulatorDir, "libshad360.so");
}