<# 
.SYNOPSIS
    Build script for shad360 - unified Xbox 360 emulator

.DESCRIPTION
    Builds both native backend (Xenia Canary via CMake) and frontend (Xenia Manager via .NET)

.PARAMETER Configuration
    Build configuration: Debug, Release, RelWithDebInfo (default: Release)

.PARAMETER BuildNative
    Build native backend only

.PARAMETER BuildFrontend
    Build frontend only

.PARAMETER RunTests
    Run tests after building

.PARAMETER Package
    Create distribution packages

.PARAMETER Clean
    Clean build directories before building

.PARAMETER Architecture
    Target architecture: x64, ARM64 (default: x64)

.PARAMETER Generator
    CMake generator: Ninja, "Visual Studio 17 2022" (default: Ninja)

.PARAMETER Verbose
    Verbose output
#>

param(
    [ValidateSet('Debug', 'Release', 'RelWithDebInfo')]
    [string]$Configuration = 'Release',

    [switch]$BuildNative,
    [switch]$BuildFrontend,
    [switch]$RunTests,
    [switch]$Package,
    [switch]$Clean,
    [ValidateSet('x64', 'ARM64')]
    [string]$Architecture = 'x64',
    [ValidateSet('Ninja', 'Visual Studio 17 2022')]
    [string]$Generator = 'Ninja',
    [switch]$Verbose
)

$ErrorActionPreference = 'Stop'

# Paths
$RepoRoot = $PSScriptRoot
$BackendSrc = Join-Path $RepoRoot 'src\backend'
$FrontendSrc = Join-Path $RepoRoot 'src\frontend'
$BackendBuild = Join-Path $RepoRoot "build\native\$Configuration"
$FrontendBuild = Join-Path $RepoRoot "build\frontend\$Configuration"
$ArtifactsDir = Join-Path $RepoRoot "artifacts\shad360-$Configuration"

# Platform detection
$IsWindows = $true
$IsMacOS = $false

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $timestamp = Get-Date -Format 'HH:mm:ss'
    $colors = @{ INFO = 'Cyan'; WARN = 'Yellow'; ERROR = 'Red'; SUCCESS = 'Green' }
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $colors[$Level]
}

function Invoke-CMake {
    param(
        [string]$SourceDir,
        [string]$BuildDir,
        [string[]]$Args
    )
    
    Write-Log "Configuring CMake in $BuildDir"
    $cmakeArgs = @(
        '-S', $SourceDir,
        '-B', $BuildDir,
        "-DCMAKE_BUILD_TYPE=$Configuration",
        "-DCMAKE_GENERATOR=$Generator",
        "-DSHAD360_BUILD_SHARED=ON",
        "-DSHAD360_ENABLE_VULKAN=ON",
        "-DSHAD360_ENABLE_D3D12=ON",
        "-DSHAD360_ENABLE_AUDIO=ON",
        "-DSHAD360_ENABLE_DISCORD=OFF"
    ) + $Args
    
    if ($Architecture -eq 'ARM64') {
        $cmakeArgs += '-DCMAKE_GENERATOR_PLATFORM=ARM64'
    }
    
    if ($Verbose) {
        $cmakeArgs += '--log-level=VERBOSE'
    }
    
    & cmake @cmakeArgs
    if ($LASTEXITCODE -ne 0) { throw "CMake configure failed" }
}

function Invoke-Build {
    param(
        [string]$BuildDir,
        [string]$Target = ''
    )
    
    Write-Log "Building in $BuildDir"
    $buildArgs = @(
        '--build', $BuildDir,
        '--config', $Configuration,
        '--parallel', $env:NUMBER_OF_PROCESSORS
    )
    if ($Target) { $buildArgs += '--target', $Target }
    if ($Verbose) { $buildArgs += '--verbose' }
    
    & cmake @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
}

function Build-Native {
    Write-Log "=== Building Native Backend ===" -Level 'SUCCESS'
    
    if ($Clean -and (Test-Path $BackendBuild)) {
        Write-Log "Cleaning $BackendBuild"
        Remove-Item -Recurse -Force $BackendBuild -ErrorAction SilentlyContinue
    }
    
    New-Item -ItemType Directory -Path $BackendBuild -Force | Out-Null
    
    # Configure
    Invoke-CMake -SourceDir $BackendSrc -BuildDir $BackendBuild
    
    # Build
    Invoke-Build -BuildDir $BackendBuild
    
    # Copy shared library to frontend
    $libName = 'shad360.dll'
    $libSrc = Join-Path $BackendBuild "bin\Windows" $libName
    $libDestDir = Join-Path $FrontendSrc "Shad360.UI\bin\$Configuration\net10.0\runtimes\win-$Architecture\native"
    New-Item -ItemType Directory -Path $libDestDir -Force | Out-Null
    
    if (Test-Path $libSrc) {
        Copy-Item $libSrc -Destination $libDestDir -Force
        Write-Log "Copied $libName to $libDestDir" -Level 'SUCCESS'
    } else {
        Write-Log "Warning: $libName not found at $libSrc" -Level 'WARN'
    }
    
    # Run tests
    if ($RunTests) {
        Write-Log "Running native tests..."
        & ctest --build-config $Configuration --output-on-failure -j $env:NUMBER_OF_PROCESSORS
        if ($LASTEXITCODE -ne 0) { throw "Native tests failed" }
    }
}

function Build-Frontend {
    Write-Log "=== Building Frontend ===" -Level 'SUCCESS'
    
    if ($Clean -and (Test-Path $FrontendBuild)) {
        Write-Log "Cleaning $FrontendBuild"
        Remove-Item -Recurse -Force $FrontendBuild -ErrorAction SilentlyContinue
    }
    
    New-Item -ItemType Directory -Path $FrontendBuild -Force | Out-Null
    
    # Restore
    Write-Log "Restoring NuGet packages..."
    & dotnet restore "$FrontendSrc\Shad360.sln"
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed" }
    
    # Build
    Write-Log "Building solution..."
    $buildArgs = @(
        'build', "$FrontendSrc\Shad360.sln",
        '--configuration', $Configuration,
        '--no-restore'
    )
    if ($Verbose) { $buildArgs += '--verbosity', 'detailed' }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Frontend build failed" }
    
    # Run tests
    if ($RunTests) {
        Write-Log "Running frontend tests..."
        & dotnet test "$FrontendSrc\Shad360.sln" --configuration $Configuration --no-build
        if ($LASTEXITCODE -ne 0) { throw "Frontend tests failed" }
    }
}

function Create-Package {
    Write-Log "=== Creating Distribution Package ===" -Level 'SUCCESS'
    
    $packageName = "shad360-$Configuration-$Architecture-windows"
    $packageDir = Join-Path $ArtifactsDir $packageName
    
    if (Test-Path $packageDir) { Remove-Item -Recurse -Force $packageDir }
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    
    # Copy frontend publish output
    $publishDir = Join-Path $FrontendSrc "Shad360.UI\bin\$Configuration\net10.0\win-$Architecture\publish"
    if (Test-Path $publishDir) {
        Write-Log "Copying frontend from $publishDir"
        Copy-Item "$publishDir\*" -Destination $packageDir -Recurse -Force
    }
    
    # Copy native library
    $libName = 'shad360.dll'
    $libSrc = Join-Path $BackendBuild "bin\Windows" $libName
    if (Test-Path $libSrc) {
        Copy-Item $libSrc -Destination $packageDir -Force
        Write-Log "Copied $libName"
    }
    
    # Copy shaders/assets
    $assetsSrc = Join-Path $BackendSrc 'assets'
    if (Test-Path $assetsSrc) {
        Copy-Item $assetsSrc -Destination (Join-Path $packageDir 'assets') -Recurse -Force
    }
    
    # Create archive
    $archivePath = Join-Path $ArtifactsDir "$packageName.zip"
    if ($IsWindows) {
        Compress-Archive -Path $packageDir -DestinationPath $archivePath -Force
    }
    
    Write-Log "Package created: $archivePath" -Level 'SUCCESS'
}

# Main
try {
    Write-Log "shad360 Build Script"
    Write-Log "Configuration: $Configuration"
    Write-Log "Architecture: $Architecture"
    Write-Log "Generator: $Generator"
    
    # Determine what to build
    $buildNative = $BuildNative -or (-not $BuildFrontend)
    $buildFrontend = $BuildFrontend -or (-not $BuildNative)
    
    if ($buildNative) {
        Build-Native
    }
    
    if ($buildFrontend) {
        Build-Frontend
    }
    
    if ($Package) {
        Create-Package
    }
    
    Write-Log "Build completed successfully!" -Level 'SUCCESS'
    
} catch {
    Write-Log "Build failed: $_" -Level 'ERROR'
    exit 1
}