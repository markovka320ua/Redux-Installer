# Redux Installer

Portable Windows installer for GTA V Redux mod.

## Features

- ✅ Self-contained - no .NET Runtime installation required
- ✅ Download and extract ZIP archives directly to GTA V folder
- ✅ Progress tracking with download speed and ETA
- ✅ Automatic GTA V detection
- ✅ Nested folder support in ZIP extraction
- ✅ Custom notification system
- ✅ Modern dark UI design
- ✅ Ukrainian localization

## Download

Latest version: [ReduxInstaller-Windows-x64-v1.0.1.zip](https://github.com/markovka320ua/Redux-Installer/releases)

## Usage

1. Download the ZIP file
2. Extract to any folder
3. Run `ReduxInstaller.exe`
4. Configure GTA V path in Settings
5. Enter mod URL and click Install

## Requirements

- Windows 10/11 x64
- ~70 MB free disk space

## Build

```bash
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

## Developer

- [markovka320](https://t.me/markovka320)

## License

MIT