cmake_minimum_required(VERSION 3.20)
project(shad360 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Target architecture detection
if(CMAKE_GENERATOR_PLATFORM)
  if(CMAKE_GENERATOR_PLATFORM STREQUAL "ARM64")
    set(XE_TARGET_AARCH64 TRUE)
    set(XE_TARGET_X86_64 FALSE)
  else()
    set(XE_TARGET_AARCH64 FALSE)
    set(XE_TARGET_X86_64 TRUE)
  endif()
elseif(CMAKE_SYSTEM_PROCESSOR MATCHES "aarch64|ARM64|arm64")
  set(XE_TARGET_AARCH64 TRUE)
  set(XE_TARGET_X86_64 FALSE)
else()
  set(XE_TARGET_AARCH64 FALSE)
  set(XE_TARGET_X86_64 TRUE)
endif()

message(STATUS "Target architecture: XE_TARGET_AARCH64=${XE_TARGET_AARCH64} XE_TARGET_X86_64=${XE_TARGET_X86_64}")

# Build options
option(SHAD360_BUILD_SHARED "Build shad360 as shared library" OFF)
option(SHAD360_BUILD_TESTS "Build test suite" OFF)
option(SHAD360_BUILD_TOOLS "Build additional tools" OFF)
option(SHAD360_ENABLE_VULKAN "Enable Vulkan backend" ON)
option(SHAD360_ENABLE_D3D12 "Enable D3D12 backend (Windows)" ON)
option(SHAD360_ENABLE_AUDIO "Enable audio backends" ON)

# Multi-config generators
get_property(_is_multi_config GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)
if(_is_multi_config)
  set(CMAKE_CONFIGURATION_TYPES "Debug;Release;RelWithDebInfo" CACHE STRING "" FORCE)
endif()

# Platform name for output paths
if(WIN32)
  set(XE_PLATFORM_NAME "Windows")
elseif(APPLE)
  set(XE_PLATFORM_NAME "macOS")
else()
  set(XE_PLATFORM_NAME "Linux")
endif()

# Output directories
string(REGEX REPLACE "/vs-[^/]+$" "" XE_OUTPUT_ROOT "${CMAKE_BINARY_DIR}")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${XE_OUTPUT_ROOT}/bin/${XE_PLATFORM_NAME}")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${XE_OUTPUT_ROOT}/bin/${XE_PLATFORM_NAME}")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/obj/${XE_PLATFORM_NAME}")

# Python for shader compilation
find_package(Python3 REQUIRED COMPONENTS Interpreter)

# Include helpers
include(cmake/XeniaHelpers.cmake)

# Global includes
include_directories(
  ${PROJECT_SOURCE_DIR}
  ${PROJECT_SOURCE_DIR}/src
  ${CMAKE_BINARY_DIR}
)
include_directories(SYSTEM ${PROJECT_SOURCE_DIR}/third_party)

# Global defines
add_compile_definitions(
  VULKAN_HPP_NO_TO_STRING
  IMGUI_DISABLE_DEFAULT_FONT
  USE_CPP17
  _LIB
  SHAD360_BUILD
)

add_compile_definitions($<$<NOT:$<CONFIG:Debug>>:IMGUI_USE_STB_SPRINTF>)

# Platform-specific settings
if(MSVC)
  add_compile_options(/utf-8 /wd4201 /MP)
  if(XE_TARGET_X86_64)
    add_compile_options(/arch:AVX)
  endif()
  add_compile_definitions(
    _CRT_NONSTDC_NO_DEPRECATE
    _CRT_SECURE_NO_WARNINGS
    WIN32
    _WIN64=1
    _AMD64=1
    UNICODE
    _UNICODE
    IMGUI_DISABLE_OBSOLETE_FUNCTIONS
  )
  add_link_options(/ignore:4006 /ignore:4221)
  link_libraries(ntdll wsock32 ws2_32 xinput comctl32 shcore shlwapi dxguid bcrypt)

  # Per-config MSVC flags
  string(APPEND CMAKE_C_FLAGS_DEBUG " /RTCsu")
  string(APPEND CMAKE_CXX_FLAGS_DEBUG " /RTCsu")
  add_compile_definitions($<$<CONFIG:Debug>:DEBUG>)
  add_compile_definitions($<$<CONFIG:Debug>:_NO_DEBUG_HEAP=1>)
  string(APPEND CMAKE_C_FLAGS_DEBUG " /Zi")
  string(APPEND CMAKE_CXX_FLAGS_DEBUG " /Zi")

  add_compile_definitions($<$<CONFIG:Release>:NDEBUG>)
  add_compile_definitions($<$<CONFIG:Release>:_NO_DEBUG_HEAP=1>)
  add_compile_options($<$<CONFIG:Release>:/Gw>)
  add_compile_options($<$<CONFIG:Release>:/GS->)
  add_compile_options($<$<CONFIG:Release>:/Zi>)
  add_link_options($<$<CONFIG:Release>:/DEBUG>)
  add_link_options($<$<CONFIG:Release>:/OPT:REF>)
  add_link_options($<$<CONFIG:Release>:/OPT:ICF>)
  string(REPLACE "/Ob2" "/Ob3" CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE}")
  string(REPLACE "/Ob2" "/Ob3" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
  add_compile_options($<$<CONFIG:Release>:/GL>)
  add_link_options($<$<CONFIG:Release>:/LTCG>)
  set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")

else()
  if(XE_TARGET_X86_64)
    add_compile_options(-mavx)
  endif()

  add_compile_options(
    $<$<COMPILE_LANGUAGE:CXX>:-Wno-switch>
    $<$<COMPILE_LANGUAGE:CXX>:-Wno-attributes>
  )

  if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    add_compile_options(
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-deprecated-register>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-deprecated-volatile>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-deprecated-enum-enum-conversion>
    )
    if(CMAKE_CXX_COMPILER_VERSION VERSION_GREATER_EQUAL 20)
      add_compile_options(
        $<$<COMPILE_LANGUAGE:CXX>:-Wno-deprecated-literal-operator>
        $<$<COMPILE_LANGUAGE:CXX>:-Wno-nontrivial-memcall>
      )
    endif()
  elseif(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    add_compile_options(
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-unused-result>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-volatile>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-template-id-cdtor>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-return-type>
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-deprecated>
      -fpermissive
    )
    add_link_options(-fpermissive)
  endif()

  add_compile_options(
    $<$<COMPILE_LANGUAGE:C>:-Wno-implicit-function-declaration>
    $<$<COMPILE_LANGUAGE:C>:-Wno-incompatible-pointer-types>
  )

  if(APPLE)
    set(CMAKE_OSX_DEPLOYMENT_TARGET "15.0" CACHE STRING "Minimum macOS version")
    add_compile_options(
      $<$<COMPILE_LANGUAGE:CXX>:-Wno-shorten-64-to-32>
      -Wno-deprecated-declarations
    )
    add_link_options(-Wl,-no_warn_duplicate_libraries)
    link_libraries("-framework CoreFoundation" "-framework Foundation" "-framework IOKit" dl pthread)
  elseif(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(GTK3 REQUIRED gtk+-x11-3.0)
    include_directories(${GTK3_INCLUDE_DIRS})
    link_directories(${GTK3_LIBRARY_DIRS})
    add_compile_options(${GTK3_CFLAGS_OTHER})
    link_libraries(stdc++fs dl lz4 pthread rt)
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,--start-group")
    set(CMAKE_CXX_STANDARD_LIBRARIES "${CMAKE_CXX_STANDARD_LIBRARIES} -Wl,--end-group")
  endif()

  string(APPEND CMAKE_C_FLAGS_DEBUG " -fsanitize=address")
  string(APPEND CMAKE_CXX_FLAGS_DEBUG " -fsanitize=address")
  string(APPEND CMAKE_EXE_LINKER_FLAGS_DEBUG " -fsanitize=address")
  add_compile_definitions($<$<CONFIG:Debug>:DEBUG>)
  if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    add_compile_definitions($<$<CONFIG:Debug>:_GLIBCXX_DEBUG>)
  endif()

  add_compile_definitions($<$<CONFIG:Release>:NDEBUG>)
  add_compile_definitions($<$<CONFIG:Release>:_NO_DEBUG_HEAP=1>)
  add_compile_options($<$<CONFIG:Release>:-O3>)
  add_compile_options($<$<CONFIG:Release>:-flto=thin>)
  add_link_options($<$<CONFIG:Release>:-flto=thin>)
  add_link_options($<$<CONFIG:Release>:-fuse-ld=lld>)
  find_program(LLVM_AR NAMES llvm-ar)
  find_program(LLVM_RANLIB NAMES llvm-ranlib)
  if(LLVM_AR)
    set(CMAKE_AR "${LLVM_AR}" CACHE FILEPATH "" FORCE)
  endif()
  if(LLVM_RANLIB)
    set(CMAKE_RANLIB "${LLVM_RANLIB}" CACHE FILEPATH "" FORCE)
  endif()
  string(REPLACE "-g" "" CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE}")
  string(REPLACE "-g" "" CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE}")
endif()

# Use folders in IDEs
set_property(GLOBAL PROPERTY USE_FOLDERS ON)

# Third-party libraries
set(CMAKE_FOLDER "third_party")
add_subdirectory(third_party)

# Xenia source modules
set(CMAKE_FOLDER "src")
add_subdirectory(src/xenia)

# Build shad360 core library (libxenia)
# This combines all xenia libraries into a single static library
add_library(shad360_core STATIC)
target_link_libraries(shad360_core PUBLIC
  xenia-core
  xenia-base
  xenia-cpu
  xenia-apu
  xenia-gpu
  xenia-hid
  xenia-kernel
  xenia-patcher
  xenia-ui
  xenia-vfs
  xenia-debug-ui
  xenia-app
  fmt
)
if(WIN32)
  target_link_libraries(shad360_core PUBLIC xenia-gpu-d3d12 xenia-apu-xaudio2 xenia-hid-xinput xenia-hid-winkey xenia-ui-d3d12)
elseif(SHAD360_ENABLE_VULKAN)
  target_link_libraries(shad360_core PUBLIC xenia-gpu-vulkan xenia-ui-vulkan)
endif()
if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  target_link_libraries(shad360_core PUBLIC xenia-apu-alsa)
endif()
if(SHAD360_ENABLE_AUDIO)
  target_link_libraries(shad360_core PUBLIC xenia-apu-sdl xenia-helper-sdl xenia-hid-sdl xenia-hid-portal)
endif()

xe_target_defaults(shad360_core)

# C API wrapper library
add_library(shad360_api STATIC
  src/xenia/api/shad360_api.h
  src/xenia/api/shad360_api.cpp
)
target_link_libraries(shad360_api PRIVATE shad360_core)
xe_target_defaults(shad360_api)

# Shared library option
if(SHAD360_BUILD_SHARED)
  add_library(shad360_shared SHARED)
  target_link_libraries(shad360_shared PRIVATE shad360_core shad360_api)
  set_target_properties(shad360_shared PROPERTIES
    OUTPUT_NAME "shad360"
    VERSION ${PROJECT_VERSION}
    SOVERSION ${PROJECT_VERSION_MAJOR}
  )
  if(WIN32)
    target_link_libraries(shad360_shared PRIVATE shad360_core shad360_api)
    target_compile_definitions(shad360_shared PRIVATE SHAD360_EXPORTS)
  endif()
endif()

# Tools
if(SHAD360_BUILD_TOOLS)
  add_subdirectory(tools)
endif()

# Tests
if(SHAD360_BUILD_TESTS)
  enable_testing()
  include(${PROJECT_SOURCE_DIR}/third_party/catch/contrib/Catch.cmake)
  add_subdirectory(tests)
endif()

# Default startup project for VS
set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT shad360_core)