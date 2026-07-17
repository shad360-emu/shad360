# shad360 C API wrapper
add_library(shad360_api STATIC
    shad360_api.h
    shad360_api.cc
)

target_include_directories(shad360_api PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/../../src
)

target_link_libraries(shad360_api PRIVATE
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
    xenia-app
    fmt
)

if(WIN32)
    target_link_libraries(shad360_api PRIVATE
        xenia-gpu-d3d12
        xenia-apu-xaudio2
        xenia-hid-xinput
        xenia-hid-winkey
        xenia-ui-d3d12
    )
elseif(SHAD360_ENABLE_VULKAN)
    target_link_libraries(shad360_api PRIVATE
        xenia-gpu-vulkan
        xenia-ui-vulkan
    )
endif()

if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
    target_link_libraries(shad360_api PRIVATE xenia-apu-alsa)
endif()

if(SHAD360_ENABLE_AUDIO)
    target_link_libraries(shad360_api PRIVATE
        xenia-apu-sdl
        xenia-helper-sdl
        xenia-hid-sdl
        xenia-hid-portal
    )
endif()

xe_target_defaults(shad360_api)

# Install headers
install(FILES shad360_api.h DESTINATION include/shad360)