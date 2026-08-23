# Redux Installer

Portable Windows installer for GTA V Redux mod.

## Features

- ✅ Self-contained - no .NET Runtime installation required
- ✅ Download and extract ZIP archives directly to GTA V folder
- ✅ Progress tracking with download speed and ETA
- ✅ Automatic GTA V detection
- ✅ Nested folder support in ZIP extraction
- ✅ Custom notification system
- ✅ Modern dark UI design with rounded corners
- ✅ Multi-language support (Ukrainian, English, Russian)
- ✅ Default language: Russian

## Download

Latest version: [ReduxInstaller-Windows-x64-v1.0.2.zip](https://github.com/markovka320ua/Redux-Installer/releases)

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

## GitHub Release Setup

To create a GitHub Release with the portable build:

1. Go to [Releases page](https://github.com/markovka320ua/Redux-Installer/releases)
2. Click "Create a new release"
3. Select tag `v1.0.2`
4. Title: `Version 1.0.2 - UI improvements and multi-language support`
5. Description:
```
## What's New

### UI Improvements
- Fixed window control buttons (32x32px, rounded corners, proper hover states)
- Increased button width for better text fit
- Added Change Location button that navigates to Settings
- Moved Settings to bottom of sidebar with separator
- Reduced sidebar font size for Install Redux button
- Reduced URL input field height
- Changed Install button text to shorter version
- Added icons to Open Logs and Clean Temp buttons
- Removed Technologies and Platform sections from About page
- Changed window border-radius to 12px

### Multi-language Support
- Added English (en-US) localization
- Added Russian (ru-RU) localization
- Set Russian as default language
- Added restart dialog when changing language

### Color Updates
- Cards: #1C1C1C
- Borders: #2A2A2A
- Accent: #E91E63

### Bug Fixes
- Fixed NotificationService with error handling
- Improved resource loading for different build scenarios
```
6. Attach `ReduxInstaller-Windows-x64-v1.0.2.zip` from the project root
7. Click "Publish release"

## Developer

- [markovka320](https://t.me/markovka320)

## License

MIT