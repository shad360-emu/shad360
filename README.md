# shad360

[![Build](https://github.com/shad360-emu/shad360/workflows/Build/badge.svg)](https://github.com/shad360-emu/shad360/actions)
[![License](https://img.shields.io/badge/License-BSD--3--Clause-blue.svg)](https://opensource.org/licenses/BSD-3-Clause)

shad360 is an open-source Xbox 360 emulator based on Xenia Canary with an integrated game manager. The emulator and frontend are built together into a single application.

## Features

- Built on Xenia Canary
- Integrated game library
- Vulkan and Direct3D 12 rendering
- Save states
- Shader cache
- Per-game settings
- Controller support
- Windows, Linux and macOS support

## Downloads

Prebuilt binaries are available from the GitHub Releases page.

## Building

Clone the repository with submodules:

```bash
git clone --recursive https://github.com/shad360-emu/shad360.git
cd shad360
```

### Windows

```powershell
.\build.ps1 -Configuration Release
```

### Linux/macOS

```bash
chmod +x build.sh
./build.sh --release
```

## Running

Launch the application and add the folder containing your Xbox 360 games.

Supported formats include:

- `.iso`
- `.xex`
- Extracted game folders

You can also launch a game directly from the command line:

```bash
shad360 game.iso
```

## Compatibility

Game compatibility varies.

See the Xenia's Compatibility Wiki for the latest information.

## Contributing

Pull requests are welcome.

Please read `CONTRIBUTING.md` before submitting changes.

## License

BSD 3-Clause.

This project includes work from:

- Xenia Canary
- Xenia Manager