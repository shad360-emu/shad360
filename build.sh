#!/usr/bin/env bash
# shad360 Build Script for Linux/macOS

set -euo pipefail

# Defaults
CONFIGURATION="Release"
BUILD_NATIVE=1
BUILD_FRONTEND=1
RUN_TESTS=0
PACKAGE=0
CLEAN=0
ARCHITECTURE="x64"
GENERATOR="Ninja"
VERBOSE=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log() { echo -e "${CYAN}[$(date +%H:%M:%S)] [INFO] $*${NC}"; }
warn() { echo -e "${YELLOW}[$(date +%H:%M:%S)] [WARN] $*${NC}"; }
error() { echo -e "${RED}[$(date +%H:%M:%S)] [ERROR] $*${NC}"; }
success() { echo -e "${GREEN}[$(date +%H:%M:%S)] [SUCCESS] $*${NC}"; }

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --debug) CONFIGURATION="Debug"; shift ;;
        --release) CONFIGURATION="Release"; shift ;;
        --relwithdebinfo) CONFIGURATION="RelWithDebInfo"; shift ;;
        --native-only) BUILD_NATIVE=1; BUILD_FRONTEND=0; shift ;;
        --frontend-only) BUILD_NATIVE=0; BUILD_FRONTEND=1; shift ;;
        --test) RUN_TESTS=1; shift ;;
        --package) PACKAGE=1; shift ;;
        --clean) CLEAN=1; shift ;;
        --arch) ARCHITECTURE="$2"; shift 2 ;;
        --generator) GENERATOR="$2"; shift 2 ;;
        --verbose) VERBOSE=1; shift ;;
        -h|--help)
            cat << EOF
Usage: $0 [options]

Options:
  --debug              Build Debug configuration
  --release            Build Release configuration (default)
  --relwithdebinfo     Build RelWithDebInfo configuration
  --native-only        Build native backend only
  --frontend-only      Build frontend only
  --test               Run tests after building
  --package            Create distribution package
  --clean              Clean build directories first
  --arch ARCH          Target architecture (x64, ARM64)
  --generator GEN      CMake generator (Ninja, "Unix Makefiles")
  --verbose            Verbose output
  -h, --help           Show this help

Environment variables:
  CONFIGURATION        Build configuration
  BUILD_NATIVE         Build native (1/0)
  BUILD_FRONTEND       Build frontend (1/0)
  RUN_TESTS            Run tests (1/0)
  PACKAGE              Create package (1/0)
  CLEAN                Clean first (1/0)
  ARCHITECTURE         Target architecture
  GENERATOR            CMake generator
  VERBOSE              Verbose output
EOF
            exit 0
            ;;
        *) error "Unknown option: $1"; exit 1 ;;
    esac
done

# Paths
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_SRC="$REPO_ROOT/src/backend"
FRONTEND_SRC="$REPO_ROOT/src/frontend"
BACKEND_BUILD="$REPO_ROOT/build/native/$CONFIGURATION"
FRONTEND_BUILD="$REPO_ROOT/build/frontend/$CONFIGURATION"
ARTIFACTS_DIR="$REPO_ROOT/artifacts/shad360-$CONFIGURATION"

# OS Detection
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="linux"
    LIB_PREFIX="lib"
    LIB_EXT="so"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    OS="macos"
    LIB_PREFIX="lib"
    LIB_EXT="dylib"
else
    error "Unsupported OS: $OSTYPE"
    exit 1
fi

# Check dependencies
check_deps() {
    log "Checking dependencies..."
    local missing=()
    command -v cmake >/dev/null || missing+=("cmake")
    command -v ninja >/dev/null || missing+=("ninja")
    command -v dotnet >/dev/null || missing+=("dotnet")
    command -v python3 >/dev/null || missing+=("python3")
    
    if [[ ${#missing[@]} -gt 0 ]]; then
        error "Missing dependencies: ${missing[*]}"
        exit 1
    fi
    success "All dependencies found"
}

# Clean build directories
clean_build() {
    if [[ "$CLEAN" == "1" ]]; then
        log "Cleaning build directories..."
        rm -rf "$BACKEND_BUILD" "$FRONTEND_BUILD"
    fi
    mkdir -p "$BACKEND_BUILD" "$FRONTEND_BUILD"
}

# Build native backend
build_native() {
    log "=== Building Native Backend ==="
    
    cd "$BACKEND_BUILD"
    
    local cmake_args=(
        -DCMAKE_BUILD_TYPE="$CONFIGURATION"
        -DCMAKE_CXX_STANDARD=20
        -DSHAD360_BUILD_SHARED=ON
        -DSHAD360_ENABLE_VULKAN=ON
        -DSHAD360_ENABLE_D3D12=OFF
        -DSHAD360_ENABLE_AUDIO=ON
        -DSHAD360_ENABLE_DISCORD=OFF
        -G "$GENERATOR"
    )
    
    if [[ "$ARCHITECTURE" == "ARM64" ]]; then
        cmake_args+=(-DCMAKE_GENERATOR_PLATFORM=ARM64)
    fi
    
    if [[ "$VERBOSE" == "1" ]]; then
        cmake_args+=(--log-level=VERBOSE)
    fi
    
    cmake "${cmake_args[@]}" "$BACKEND_SRC"
    cmake --build . --config "$CONFIGURATION" --parallel "$(nproc)"
    
    # Copy library to frontend
    local lib_name="${LIB_PREFIX}shad360.${LIB_EXT}"
    local lib_src="$BACKEND_BUILD/bin/$OS/$lib_name"
    local lib_dest_dir="$FRONTEND_SRC/Shad360.UI/bin/$CONFIGURATION/net10.0/runtimes/${OS}-${ARCHITECTURE}/native"
    
    mkdir -p "$lib_dest_dir"
    if [[ -f "$lib_src" ]]; then
        cp "$lib_src" "$lib_dest_dir/"
        success "Copied $lib_name to $lib_dest_dir"
    else
        warn "Library not found at $lib_src"
    fi
    
    # Run tests
    if [[ "$RUN_TESTS" == "1" ]]; then
        log "Running native tests..."
        ctest --build-config "$CONFIGURATION" --output-on-failure --parallel "$(nproc)"
    fi
}

# Build frontend
build_frontend() {
    log "=== Building Frontend ==="
    
    cd "$FRONTEND_SRC"
    
    # Restore
    log "Restoring NuGet packages..."
    dotnet restore Shad360.sln
    
    # Build
    log "Building solution..."
    local build_args=(build Shad360.sln --configuration "$CONFIGURATION" --no-restore)
    if [[ "$VERBOSE" == "1" ]]; then
        build_args+=(--verbosity detailed)
    fi
    dotnet "${build_args[@]}"
    
    # Run tests
    if [[ "$RUN_TESTS" == "1" ]]; then
        log "Running frontend tests..."
        dotnet test Shad360.sln --configuration "$CONFIGURATION" --no-build
    fi
}

# Create distribution package
create_package() {
    log "=== Creating Distribution Package ==="
    
    local package_name="shad360-$CONFIGURATION-${ARCHITECTURE}-$OS"
    local package_dir="$ARTIFACTS_DIR/$package_name"
    
    rm -rf "$package_dir"
    mkdir -p "$package_dir"
    
    # Copy frontend publish output
    local publish_dir="$FRONTEND_SRC/Shad360.UI/bin/$CONFIGURATION/net10.0/${OS}-${ARCHITECTURE}/publish"
    if [[ -d "$publish_dir" ]]; then
        log "Copying frontend from $publish_dir"
        cp -r "$publish_dir"/* "$package_dir/"
    else
        warn "Publish directory not found: $publish_dir"
    fi
    
    # Copy native library
    local lib_name="${LIB_PREFIX}shad360.${LIB_EXT}"
    local lib_src="$BACKEND_BUILD/bin/$OS/$lib_name"
    if [[ -f "$lib_src" ]]; then
        cp "$lib_src" "$package_dir/"
        success "Copied $lib_name"
    fi
    
    # Copy assets
    local assets_src="$BACKEND_SRC/assets"
    if [[ -d "$assets_src" ]]; then
        cp -r "$assets_src" "$package_dir/"
    fi
    
    # Create archive
    local archive_path="$ARTIFACTS_DIR/${package_name}.tar.gz"
    tar -czf "$archive_path" -C "$package_dir" .
    success "Package created: $archive_path"
}

# Main
main() {
    log "shad360 Build Script"
    log "Configuration: $CONFIGURATION"
    log "Architecture: $ARCHITECTURE"
    log "OS: $OS"
    log "Generator: $GENERATOR"
    
    check_deps
    clean_build
    
    if [[ "$BUILD_NATIVE" == "1" ]]; then
        build_native
    fi
    
    if [[ "$BUILD_FRONTEND" == "1" ]]; then
        build_frontend
    fi
    
    if [[ "$PACKAGE" == "1" ]]; then
        create_package
    fi
    
    success "Build completed successfully!"
}

main "$@"