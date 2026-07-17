/**
 ******************************************************************************
 * shad360 : Xbox 360 Emulator Research Project                               *
 ******************************************************************************
 * Copyright 2024 shad360-emu. All rights reserved.                           *
 * Released under the BSD license - see LICENSE in the root for more details. *
 ******************************************************************************
 */

#ifndef SHAD360_API_H_
#define SHAD360_API_H_

#include <stdint.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

// Version info
#define SHAD360_VERSION_MAJOR 0
#define SHAD360_VERSION_MINOR 1
#define SHAD360_VERSION_PATCH 0

// Platform-specific export/import
#if defined(_WIN32) && defined(SHAD360_EXPORTS)
#  define SHAD360_API __declspec(dllexport)
#elif defined(_WIN32) && !defined(SHAD360_STATIC)
#  define SHAD360_API __declspec(dllimport)
#else
#  define SHAD360_API
#endif

// Error codes
typedef enum shad360_status {
  SHAD360_STATUS_SUCCESS = 0,
  SHAD360_STATUS_ERROR = -1,
  SHAD360_STATUS_INVALID_ARGUMENT = -2,
  SHAD360_STATUS_NOT_INITIALIZED = -3,
  SHAD360_STATUS_ALREADY_INITIALIZED = -4,
  SHAD360_STATUS_FILE_NOT_FOUND = -5,
  SHAD360_STATUS_OUT_OF_MEMORY = -6,
  SHAD360_STATUS_GAME_ALREADY_RUNNING = -7,
  SHAD360_STATUS_NO_GAME_RUNNING = -8,
  SHAD360_STATUS_UNSUPPORTED_FILE = -9,
  SHAD360_STATUS_EMULATOR_ERROR = -10,
} shad360_status_t;

// Graphics backend
typedef enum shad360_gfx_backend {
  SHAD360_GFX_AUTO = 0,
  SHAD360_GFX_VULKAN = 1,
  SHAD360_GFX_D3D12 = 2,
  SHAD360_GFX_NULL = 3,
} shad360_gfx_backend_t;

// Audio backend
typedef enum shad360_audio_backend {
  SHAD360_AUDIO_AUTO = 0,
  SHAD360_AUDIO_XAUDIO2 = 1,
  SHAD360_AUDIO_SDL = 2,
  SHAD360_AUDIO_NULL = 3,
} shad360_audio_backend_t;

// Emulator configuration
typedef struct shad360_config {
  const char* storage_path;
  const char* content_path;
  const char* cache_path;
  const char* game_path;
  shad360_gfx_backend_t gfx_backend;
  shad360_audio_backend_t audio_backend;
  int enable_debug_ui;
  int enable_vsync;
  int fullscreen;
  int resolution_width;
  int resolution_height;
  int cpu_threads;
  int enable_jit;
  int enable_hle;
  float cpu_accuracy;
  float gpu_accuracy;
} shad360_config_t;

// Emulator state
typedef enum shad360_state {
  SHAD360_STATE_UNINITIALIZED = 0,
  SHAD360_STATE_READY = 1,
  SHAD360_STATE_RUNNING = 2,
  SHAD360_STATE_PAUSED = 3,
  SHAD360_STATE_STOPPING = 4,
  SHAD360_STATE_ERROR = 5,
} shad360_state_t;

// Game info
typedef struct shad360_game_info {
  char title_name[256];
  char title_id[16];
  char region[16];
  char developer[128];
  char publisher[128];
  char genre[64];
  uint32_t title_id_hex;
  int is_demo;
  int is_xbla;
  int is_xbox360;
} shad360_game_info_t;

// Emulator stats
typedef struct shad360_stats {
  uint64_t frame_count;
  double fps;
  double frame_time_ms;
  uint64_t cpu_cycles;
  uint64_t gpu_draw_calls;
  uint64_t gpu_triangles;
  uint64_t memory_used_mb;
  uint64_t audio_buffer_underruns;
} shad360_stats_t;

// Opaque emulator handle
typedef struct shad360_emulator shad360_emulator_t;

// Callback types
typedef void (*shad360_log_callback_t)(int level, const char* message, void* user_data);
typedef void (*shad360_state_callback_t)(shad360_state_t state, void* user_data);
typedef void (*shad360_game_event_callback_t)(const char* event, const char* data, void* user_data);
typedef void (*shad360_frame_callback_t)(const void* frame_data, int width, int height, int stride, void* user_data);

// Initialize emulator
SHAD360_API shad360_status_t shad360_init(const shad360_config_t* config, shad360_emulator_t** out_emulator);

// Shutdown emulator
SHAD360_API shad360_status_t shad360_shutdown(shad360_emulator_t* emulator);

// Get emulator state
SHAD360_API shad360_state_t shad360_get_state(shad360_emulator_t* emulator);

// Launch a game
SHAD360_API shad360_status_t shad360_launch_game(shad360_emulator_t* emulator, const char* game_path);

// Stop current game
SHAD360_API shad360_status_t shad360_stop_game(shad360_emulator_t* emulator);

// Pause/resume emulation
SHAD360_API shad360_status_t shad360_pause(shad360_emulator_t* emulator);
SHAD360_API shad360_status_t shad360_resume(shad360_emulator_t* emulator);

// Step single frame
SHAD360_API shad360_status_t shad360_step_frame(shad360_emulator_t* emulator);

// Get game info
SHAD360_API shad360_status_t shad360_get_game_info(shad360_emulator_t* emulator, shad360_game_info_t* out_info);

// Get emulator stats
SHAD360_API shad360_status_t shad360_get_stats(shad360_emulator_t* emulator, shad360_stats_t* out_stats);

// Save/load state
SHAD360_API shad360_status_t shad360_save_state(shad360_emulator_t* emulator, const char* path);
SHAD360_API shad360_status_t shad360_load_state(shad360_emulator_t* emulator, const char* path);

// Set callbacks
SHAD360_API void shad360_set_log_callback(shad360_log_callback_t callback, void* user_data);
SHAD360_API void shad360_set_state_callback(shad360_state_callback_t callback, void* user_data);
SHAD360_API void shad360_set_game_event_callback(shad360_game_event_callback_t callback, void* user_data);
SHAD360_API void shad360_set_frame_callback(shad360_frame_callback_t callback, void* user_data);

// Configuration helpers
SHAD360_API void shad360_config_default(shad360_config_t* config);
SHAD360_API const char* shad360_status_string(shad360_status_t status);
SHAD360_API const char* shad360_state_string(shad360_state_t state);
SHAD360_API const char* shad360_version_string(void);

// GPU/CPU backend info
SHAD360_API const char** shad360_get_available_gfx_backends(int* out_count);
SHAD360_API const char** shad360_get_available_audio_backends(int* out_count);

// Controller/input
typedef enum shad360_controller_port {
  SHAD360_PORT_1 = 0,
  SHAD360_PORT_2 = 1,
  SHAD360_PORT_3 = 2,
  SHAD360_PORT_4 = 3,
} shad360_controller_port_t;

typedef struct shad360_controller_state {
  uint16_t buttons;
  int16_t left_trigger;
  int16_t right_trigger;
  int16_t left_thumb_x;
  int16_t left_thumb_y;
  int16_t right_thumb_x;
  int16_t right_thumb_y;
  int connected;
} shad360_controller_state_t;

SHAD360_API shad360_status_t shad360_set_controller_state(shad360_emulator_t* emulator, shad360_controller_port_t port, const shad360_controller_state_t* state);
SHAD360_API shad360_status_t shad360_get_controller_state(shad360_emulator_t* emulator, shad360_controller_port_t port, shad360_controller_state_t* out_state);

// Memory operations
SHAD360_API shad360_status_t shad360_read_memory(shad360_emulator_t* emulator, uint64_t address, void* buffer, size_t size);
SHAD360_API shad360_status_t shad360_write_memory(shad360_emulator_t* emulator, uint64_t address, const void* buffer, size_t size);

// Debug
SHAD360_API shad360_status_t shad360_debug_break(shad360_emulator_t* emulator);
SHAD360_API shad360_status_t shad360_debug_continue(shad360_emulator_t* emulator);
SHAD360_API shad360_status_t shad360_debug_step(shad360_emulator_t* emulator);

// Module/DLL loading
SHAD360_API shad360_status_t shad360_load_module(shad360_emulator_t* emulator, const char* path);

// Shader cache
SHAD360_API shad360_status_t shad360_clear_shader_cache(shad360_emulator_t* emulator);

#ifdef __cplusplus
}
#endif

#endif  // SHAD360_API_H_