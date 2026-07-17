# shad360 - Xbox 360 Emulator Research Project

cmake_minimum_required(VERSION 3.20)
project(shad360 VERSION 0.1.0 LANGUAGES C CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)

# Policy settings
cmake_policy(SET CMP0077 NEW)  # option() honors normal variables
cmake_policy(SET CMP0063 NEW)  # Honor visibility properties

# Detect target architecture
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
option(SHAD360_ENABLE_DISCORD "Enable Discord RPC integration" ON)
option(SHAD360_EMBED_FRONTEND "Embed .NET frontend resources" ON)

# Multi-config generators (VS, Ninja Multi-Config)
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
  SHAD360_VERSION_MAJOR=0
  SHAD360_VERSION_MINOR=1
  SHAD360_VERSION_PATCH=0
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

# =============================================================================
# Third-party libraries
# =============================================================================
set(CMAKE_FOLDER "third_party")
add_subdirectory(src/backend/third_party)

# =============================================================================
# Xenia source modules (shad360 backend)
# =============================================================================
set(CMAKE_FOLDER "src/backend")
add_subdirectory(src/backend/src/xenia)

# =============================================================================
# shad360 core library (libxenia)
# =============================================================================
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
  target_link_libraries(shad360_core PUBLIC
    xenia-gpu-d3d12
    xenia-apu-xaudio2
    xenia-hid-xinput
    xenia-hid-winkey
    xenia-ui-d3d12
  )
elseif(SHAD360_ENABLE_VULKAN)
  target_link_libraries(shad360_core PUBLIC
    xenia-gpu-vulkan
    xenia-ui-vulkan
    glslang-spirv
  )
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  target_link_libraries(shad360_core PUBLIC xenia-apu-alsa)
endif()

if(SHAD360_ENABLE_AUDIO)
  target_link_libraries(shad360_core PUBLIC
    xenia-apu-sdl
    xenia-helper-sdl
    xenia-hid-sdl
    xenia-hid-portal
  )
endif()

if(SHAD360_ENABLE_DISCORD)
  target_link_libraries(shad360_core PUBLIC xenia-app-discord discord-rpc)
endif()

xe_target_defaults(shad360_core)

# C API wrapper library
add_library(shad360_api STATIC
  src/backend/src/xenia/api/shad360_api.h
  src/backend/src/xenia/api/shad360_api.cc
)
target_include_directories(shad360_api PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}/src/backend/src/xenia/api
  ${CMAKE_CURRENT_SOURCE_DIR}/src/backend/src
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
    POSITION_INDEPENDENT_CODE ON
  )
  if(WIN32)
    target_compile_definitions(shad360_shared PRIVATE SHAD360_EXPORTS)
  endif()
  install(TARGETS shad360_shared
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin
    ARCHIVE DESTINATION lib
  )
  install(FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/backend/src/xenia/api/shad360_api.h
    DESTINATION include/shad360
  )
endif()

# =============================================================================
# Tools
# =============================================================================
if(SHAD360_BUILD_TOOLS)
  add_subdirectory(src/backend/tools)
endif()

# =============================================================================
# Tests
# =============================================================================
if(SHAD360_BUILD_TESTS)
  enable_testing()
  include(${CMAKE_CURRENT_SOURCE_DIR}/src/backend/third_party/catch/contrib/Catch.cmake)
  add_subdirectory(src/backend/tests)
endif()

# =============================================================================
# Frontend (.NET) - handled separately via dotnet build
# =============================================================================
if(SHAD360_EMBED_FRONTEND)
  add_custom_target(shad360-frontend
    COMMAND ${CMAKE_COMMAND} -E echo "Frontend built separately via 'dotnet build src/frontend/Shad360.sln'"
    COMMENT "Frontend is built with .NET SDK"
    VERBATIM
  )
endif()

# =============================================================================
# Installation
# =============================================================================
install(TARGETS shad360_core shad360_api
  LIBRARY DESTINATION lib
  ARCHIVE DESTINATION lib
)

install(DIRECTORY src/backend/src/xenia/api/
  DESTINATION include/shad360
  FILES_MATCHING PATTERN "*.h"
)

# =============================================================================
# Package configuration
# =============================================================================
include(CMakePackageConfigHelpers)
configure_package_config_file(
  ${CMAKE_CURRENT_SOURCE_DIR}/cmake/shad360Config.cmake.in
  ${CMAKE_CURRENT_BINARY_DIR}/shad360Config.cmake
  INSTALL_DESTINATION lib/cmake/shad360
)

write_basic_package_version_file(
  ${CMAKE_CURRENT_BINARY_DIR}/shad360ConfigVersion.cmake
  VERSION ${PROJECT_VERSION}
  COMPATIBILITY SameMajorVersion
)

install(FILES
  ${CMAKE_CURRENT_BINARY_DIR}/shad360Config.cmake
  ${CMAKE_CURRENT_BINARY_DIR}/shad360ConfigVersion.cmake
  DESTINATION lib/cmake/shad360
)

# Default startup project for VS
set_property(DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY VS_STARTUP_PROJECT shad360_core)

# Print summary
message(STATUS "========================================")
message(STATUS "shad360 Build Configuration")
message(STATUS "========================================")
message(STATUS "Version: ${PROJECT_VERSION}")
message(STATUS "Platform: ${XE_PLATFORM_NAME}")
message(STATUS "Architecture: ${CMAKE_SYSTEM_PROCESSOR}")
message(STATUS "Target: X86_64=${XE_TARGET_X86_64} AARCH64=${XE_TARGET_AARCH64}")
message(STATUS "Shared Library: ${SHAD360_BUILD_SHARED}")
message(STATUS "Vulkan: ${SHAD360_ENABLE_VULKAN}")
message(STATUS "D3D12: ${SHAD360_ENABLE_D3D12}")
message(STATUS "Audio: ${SHAD360_ENABLE_AUDIO}")
message(STATUS "Discord RPC: ${SHAD360_ENABLE_DISCORD}")
message(STATUS "Tests: ${SHAD360_BUILD_TESTS}")
message(STATUS "Tools: ${SHAD360_BUILD_TOOLS}")
message(STATUS "========================================")