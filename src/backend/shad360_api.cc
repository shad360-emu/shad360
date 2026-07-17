#include "shad360_api.h"

#include <xenia/emulator.h>
#include <xenia/base/logging.h>
#include <xenia/base/filesystem.h>
#include <xenia/ui/window.h>
#include <xenia/ui/gl_window.h>
#include <xenia/ui/vulkan_window.h>

#include <memory>
#include <string>
#include <vector>
#include <mutex>
#include <map>

namespace {

struct Shad360Context {
    std::unique_ptr<xe::Emulator> emulator;
    std::unique_ptr<xe::ui::Window> window;
    xe::filesystem::Path storage_root;
    xe::filesystem::Path content_root;
    xe::filesystem::Path cache_root;
    std::mutex mutex;
    bool initialized = false;
    bool running = false;
    int window_width = 1280;
    int window_height = 720;
    std::string window_title = "shad360 - Xbox 360 Emulator";
    void (*log_callback)(int level, const char* message) = nullptr;
    void (*game_event_callback)(int event_type, const char* game_id, const char* message) = nullptr;
};

std::map<int, std::unique_ptr<Shad360Context>> g_contexts;
std::mutex g_contexts_mutex;
int g_next_context_id = 1;

void LogCallback(int level, const char* message) {
    if (!message) return;
    
    // Map xe log levels to shad360 levels
    // 0 = Debug, 1 = Info, 2 = Warning, 3 = Error, 4 = Fatal
    xe::LogLevel xe_level;
    switch (level) {
        case 0: xe_level = xe::LogLevel::Debug; break;
        case 1: xe_level = xe::LogLevel::Info; break;
        case 2: xe_level = xe::LogLevel::Warning; break;
        case 3: xe_level = xe::LogLevel::Error; break;
        case 4: xe_level = xe::LogLevel::Fatal; break;
        default: xe_level = xe::LogLevel::Info; break;
    }
    XELOG(xe_level) << message;
}

Shad360Context* GetContext(int context_id) {
    std::lock_guard<std::mutex> lock(g_contexts_mutex);
    auto it = g_contexts.find(context_id);
    return it != g_contexts.end() ? it->second.get() : nullptr;
}

int CreateContextInternal() {
    std::lock_guard<std::mutex> lock(g_contexts_mutex);
    int id = g_next_context_id++;
    g_contexts[id] = std::make_unique<Shad360Context>();
    return id;
}

void DestroyContextInternal(int context_id) {
    std::lock_guard<std::mutex> lock(g_contexts_mutex);
    g_contexts.erase(context_id);
}

xe::X_STATUS LaunchGameInternal(Shad360Context* ctx, const char* path) {
    if (!ctx || !ctx->emulator || !path) {
        return xe::X_STATUS_INVALID_PARAMETER;
    }
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    if (!ctx->initialized) {
        return xe::X_STATUS_NOT_INITIALIZED;
    }
    
    xe::filesystem::Path game_path(path);
    return ctx->emulator->LaunchPath(game_path);
}

}  // namespace

// Public C API

SHAD360_API int shad360_initialize(const char* storage_path, const char* content_path, const char* cache_path) {
    int context_id = CreateContextInternal();
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) {
        return -1;
    }
    
    try {
        if (storage_path) ctx->storage_root = storage_path;
        if (content_path) ctx->content_root = content_path;
        if (cache_path) ctx->cache_root = cache_path;
        
        // Create emulator
        ctx->emulator = std::make_unique<xe::Emulator>(
            xe::filesystem::Path("shad360"),
            ctx->storage_root.empty() ? xe::filesystem::Path(xe::base::GetUserDataDirectory() / "shad360" / "storage") : ctx->storage_root,
            ctx->content_root.empty() ? xe::filesystem::Path(xe::base::GetUserDataDirectory() / "shad360" / "content") : ctx->content_root,
            ctx->cache_root.empty() ? xe::filesystem::Path(xe::base::GetUserDataDirectory() / "shad360" / "cache") : ctx->cache_root
        );
        
        ctx->initialized = true;
        
        if (ctx->log_callback) {
            ctx->log_callback(1, "shad360 initialized successfully");
        }
        
        return context_id;
    } catch (const std::exception& e) {
        if (ctx->log_callback) {
            ctx->log_callback(3, e.what());
        }
        DestroyContextInternal(context_id);
        return -1;
    }
}

SHAD360_API void shad360_shutdown(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    
    if (ctx->running) {
        ctx->emulator->TerminateTitle();
        ctx->running = false;
    }
    
    ctx->emulator.reset();
    ctx->window.reset();
    ctx->initialized = false;
    
    DestroyContextInternal(context_id);
}

SHAD360_API int shad360_create_window(int context_id, int width, int height, const char* title, int backend) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->initialized) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    
    ctx->window_width = width > 0 ? width : 1280;
    ctx->window_height = height > 0 ? height : 720;
    ctx->window_title = title ? title : "shad360 - Xbox 360 Emulator";
    
    try {
        xe::ui::Window::CreateParams params;
        params.title = ctx->window_title;
        params.width = ctx->window_width;
        params.height = ctx->window_height;
        params.fullscreen = false;
        params.resizable = true;
        params.vsync = true;
        
        // Choose backend based on platform and preference
        #if defined(XE_PLATFORM_WINDOWS)
        if (backend == 1) {  // D3D12
            params.preferred_graphics_api = xe::ui::GraphicsAPI::kD3D12;
        } else {
            params.preferred_graphics_api = xe::ui::GraphicsAPI::kVulkan;
        }
        #else
        params.preferred_graphics_api = xe::ui::GraphicsAPI::kVulkan;
        #endif
        
        ctx->window = xe::ui::Window::Create(params);
        if (!ctx->window) {
            if (ctx->log_callback) ctx->log_callback(3, "Failed to create window");
            return -1;
        }
        
        // Setup emulator with window
        auto audio_factory = [](xe::cpu::Processor* processor) -> std::unique_ptr<xe::apu::AudioSystem> {
            #if defined(XE_PLATFORM_WINDOWS)
            return std::make_unique<xe::apu::XAudio2AudioSystem>(processor);
            #else
            return std::make_unique<xe::apu::NopAudioSystem>(processor);
            #endif
        };
        
        auto graphics_factory = [ctx]() -> std::unique_ptr<xe::gpu::GraphicsSystem> {
            if (!ctx->window) return std::make_unique<xe::gpu::NullGraphicsSystem>();
            
            #if defined(XE_PLATFORM_WINDOWS)
            if (ctx->window->graphics_api() == xe::ui::GraphicsAPI::kD3D12) {
                return std::make_unique<xe::gpu::d3d12::D3D12GraphicsSystem>(ctx->window.get());
            }
            #endif
            return std::make_unique<xe::gpu::vulkan::VulkanGraphicsSystem>(ctx->window.get());
        };
        
        auto input_factory = [ctx](xe::ui::Window* window) -> std::vector<std::unique_ptr<xe::hid::InputDriver>> {
            std::vector<std::unique_ptr<xe::hid::InputDriver>> drivers;
            
            #if defined(XE_PLATFORM_WINDOWS)
            drivers.emplace_back(std::make_unique<xe::hid::winkey::WinKeyInputDriver>(window));
            drivers.emplace_back(std::make_unique<xe::hid::xinput::XInputInputDriver>(window));
            #endif
            
            drivers.emplace_back(std::make_unique<xe::hid::sdl::SDLInputDriver>(window));
            return drivers;
        };
        
        xe::X_STATUS status = ctx->emulator->Setup(
            ctx->window.get(),
            nullptr,  // ImGui drawer (handled by UI layer)
            true,     // require_cpu_backend
            audio_factory,
            graphics_factory,
            input_factory
        );
        
        if (status != xe::X_STATUS_SUCCESS) {
            if (ctx->log_callback) ctx->log_callback(3, "Failed to setup emulator");
            return -1;
        }
        
        if (ctx->log_callback) ctx->log_callback(1, "Window created and emulator setup complete");
        return 0;
        
    } catch (const std::exception& e) {
        if (ctx->log_callback) ctx->log_callback(3, e.what());
        return -1;
    }
}

SHAD360_API void shad360_destroy_window(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->window.reset();
}

SHAD360_API int shad360_launch_game(int context_id, const char* path) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return -1;
    
    xe::X_STATUS status = LaunchGameInternal(ctx, path);
    if (status == xe::X_STATUS_SUCCESS) {
        std::lock_guard<std::mutex> lock(ctx->mutex);
        ctx->running = true;
        if (ctx->game_event_callback) {
            ctx->game_event_callback(0, nullptr, "Game launched");
        }
        return 0;
    }
    return -1;
}

SHAD360_API int shad360_terminate_game(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    xe::X_STATUS status = ctx->emulator->TerminateTitle();
    ctx->running = false;
    
    if (ctx->game_event_callback) {
        ctx->game_event_callback(1, nullptr, "Game terminated");
    }
    
    return status == xe::X_STATUS_SUCCESS ? 0 : -1;
}

SHAD360_API int shad360_pause(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->emulator->Pause();
    return 0;
}

SHAD360_API int shad360_resume(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->emulator->Resume();
    return 0;
}

SHAD360_API int shad360_is_running(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return 0;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    return ctx->running && ctx->emulator->is_title_open() ? 1 : 0;
}

SHAD360_API int shad360_is_paused(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return 0;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    return ctx->emulator->is_paused() ? 1 : 0;
}

SHAD360_API const char* shad360_get_game_title(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return nullptr;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    static thread_local std::string title;
    title = ctx->emulator->title_name();
    return title.c_str();
}

SHAD360_API const char* shad360_get_game_version(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return nullptr;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    static thread_local std::string version;
    version = ctx->emulator->title_version();
    return version.c_str();
}

SHAD360_API uint32_t shad360_get_title_id(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator) return 0;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    return ctx->emulator->title_id();
}

SHAD360_API int shad360_save_state(int context_id, const char* path) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator || !path) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    xe::filesystem::Path save_path(path);
    return ctx->emulator->SaveToFile(save_path) ? 0 : -1;
}

SHAD360_API int shad360_load_state(int context_id, const char* path) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator || !path) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    xe::filesystem::Path load_path(path);
    return ctx->emulator->RestoreFromFile(load_path) ? 0 : -1;
}

SHAD360_API void shad360_set_log_callback(int context_id, void (*callback)(int level, const char* message)) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->log_callback = callback;
}

SHAD360_API void shad360_set_game_event_callback(int context_id, void (*callback)(int event_type, const char* game_id, const char* message)) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx) return;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->game_event_callback = callback;
}

SHAD360_API int shad360_mount_path(int context_id, const char* path, const char* mount_point) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator || !path || !mount_point) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    xe::filesystem::Path fs_path(path);
    return ctx->emulator->MountPath(fs_path, mount_point) == xe::X_STATUS_SUCCESS ? 0 : -1;
}

SHAD360_API int shad360_unmount_path(int context_id, const char* mount_point) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->emulator || !mount_point) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    // VFS doesn't have direct unmount, would need to use file_system()->Unmount()
    return 0;
}

SHAD360_API int shad360_get_version(int* major, int* minor, int* patch) {
    if (major) *major = 0;
    if (minor) *minor = 1;
    if (patch) *patch = 0;
    return 0;
}

SHAD360_API const char* shad360_get_version_string() {
    return "shad360 0.1.0 (Xenia Canary based)";
}

SHAD360_API int shad360_set_config(int context_id, const char* key, const char* value) {
    // Config handling would go here
    return 0;
}

SHAD360_API const char* shad360_get_config(int context_id, const char* key) {
    return nullptr;
}

SHAD360_API int shad360_take_screenshot(int context_id, const char* path) {
    // Screenshot implementation
    return 0;
}

SHAD360_API int shad360_set_fullscreen(int context_id, int fullscreen) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->window) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    ctx->window->SetFullscreen(fullscreen != 0);
    return 0;
}

SHAD360_API int shad360_get_window_size(int context_id, int* width, int* height) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->window) return -1;
    
    std::lock_guard<std::mutex> lock(ctx->mutex);
    if (width) *width = ctx->window->width();
    if (height) *height = ctx->window->height();
    return 0;
}

SHAD360_API int shad360_poll_events(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->window) return -1;
    
    ctx->window->PollEvents();
    return 0;
}

SHAD360_API int shad360_present(int context_id) {
    Shad360Context* ctx = GetContext(context_id);
    if (!ctx || !ctx->window) return -1;
    
    ctx->window->Present();
    return 0;
}