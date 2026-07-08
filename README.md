# SpiderNoir Jellyfin Plugin

Switch between Black & White (Noir) and Color versions of SpiderNoir series episodes directly from the Jellyfin video player.

## Features

- **Version Detection** — Automatically detects episodes that have both Noir and Color versions using suffix-based or folder-based naming conventions.
- **Player Overlay** — A DVD icon toggle button in the video player opens a dropdown menu to switch versions with a single click.
- **REST API** — Query available versions, switch between them, and list detected series.
- **Dashboard Configuration** — Configure naming conventions, auto-detection, and overlay behavior from the Jellyfin dashboard.

## Prerequisites

- Jellyfin server 10.9 or newer
- Jellyfin web client (for the player overlay)

## Installation

### Option 1: Plugin Repository (Recommended)

1. Go to **Dashboard -> Plugins -> Repositories**
2. Click **+ Add**
3. Enter:
   - **Name**: `SpiderNoir`
   - **URL**: `https://raw.githubusercontent.com/zakattack02/Noir-plugin/develop/manifest.json`
4. Click **Save**
5. Go to **Catalog**, find **SpiderNoir**, click **Install**

### Option 2: Manual Install

1. Download the latest `.zip` from [GitHub Releases](https://github.com/zakattack02/Noir-plugin/releases)
2. Extract `Jellyfin.Plugin.SpiderNoir.dll`
3. Copy it to your Jellyfin plugins directory:
   - **Linux**: `~/.local/share/jellyfin/plugins/SpiderNoir/`
   - **Windows**: `%LOCALAPPDATA%/jellyfin/plugins/SpiderNoir/`
4. Restart Jellyfin

### Enable the Player Overlay

The overlay script is served by the plugin. To activate it:

1. Go to **Dashboard -> General**
2. Scroll to **Custom JavaScript**
3. Paste: `/SpiderNoir/player-overlay.js`
4. Click **Save**

The DVD icon will appear in the bottom-right of the video player when playing SpiderNoir content with detected version pairs.

## Quick Start

1. Install the plugin (see above)
2. Add `/SpiderNoir/player-overlay.js` to Custom JavaScript
3. Place your media files using one of the naming conventions below
4. Play a SpiderNoir episode
5. Click the DVD icon -> select a version -> playback switches with position preserved

## Naming Conventions

### Suffix-Based (Default)

Untagged files are treated as the Color version. Add a suffix for the alternate version:

```
SpiderNoir S01E01.mkv                   Color (untagged = Color)
SpiderNoir S01E01 - Noir.mkv            Noir version
SpiderNoir S01E01 - Color.mkv           Explicit Color tag
SpiderNoir S01E01 - B&W.mkv             Alternate Noir tag
```

### Folder-Based

Episodes are stored in separate subdirectories:

```
Series/Season 01/Color/S01E01.mkv
Series/Season 01/Noir/S01E01.mkv
```

## Configuration

Access the configuration page at **Dashboard -> Plugins -> SpiderNoir**.

| Setting | Type | Default | Description |
|---|---|---|---|
| Version Naming Convention | enum | SuffixBased | How to pair Noir/Color episodes (SuffixBased, FolderBased, Custom) |
| Noir (B&W) Suffix | string | Noir | File suffix for the Black & White version |
| Color Suffix | string | Color | File suffix for the Color version |
| Series Name Pattern | string | SpiderNoir | Series name substring to match for auto-detection |
| Auto-Detect SpiderNoir Series | bool | true | Automatically scan for series matching the name pattern |
| Enable Player Overlay | bool | true | Show the version switch overlay in the video player |

## Usage

The player overlay adds a DVD icon button to the bottom-right of the video player. Clicking it opens a dropdown menu showing Noir and Color options. The active version has a checkmark. Select the other version to switch; playback reloads with the new file while preserving your current position.

The overlay only appears when:
- The plugin is installed and Custom JavaScript is configured
- The currently playing item belongs to a detected SpiderNoir series
- Both Noir and Color versions exist for that episode

## API Overview

All endpoints are under `/{JellyfinUrl}/SpiderNoir/`. Endpoints returning media files (`player-overlay.js`, `icon.svg`) are public. All others require a Jellyfin auth token.

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | /SpiderNoir/versions/{itemId} | Yes | Get version info for an episode |
| POST | /SpiderNoir/switch/{itemId}/{version} | Yes | Switch to a different version |
| GET | /SpiderNoir/series | Yes | List all detected series with version info |
| GET | /SpiderNoir/player-overlay.js | No | Serve the player overlay script |
| GET | /SpiderNoir/icon.svg | No | Serve the plugin icon |

See the [API documentation](wiki/api.md) for full details.

## Project Structure

```
Jellyfin.Plugin.SpiderNoir/
├── Api/
│   └── SpiderNoirController.cs       REST API (5 endpoints)
├── Configuration/
│   ├── PluginConfiguration.cs        Settings model
│   └── configPage.html               Dashboard config UI (embedded)
├── Services/
│   ├── VersionDetectionService.cs    Scans library for version pairs
│   ├── EpisodeVersionPair.cs         Data model
│   └── PluginServiceRegistrator.cs   DI registration
├── Web/
│   └── playerOverlay.js              Player overlay script (embedded)
├── Plugin.cs                         Main plugin class
├── Jellyfin.Plugin.SpiderNoir.csproj Project file (net9.0)
├── build.yaml                        CI metadata
└── manifest.json                     Plugin repo manifest
```

## Build from Source

```bash
# Build
dotnet build Jellyfin.Plugin.SpiderNoir.sln

# Publish Release
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Release
```

- No unit tests currently. CI delegates to `jellyfin/jellyfin-meta-plugins`.
- Targets `net9.0` with nullable enabled, `TreatWarningsAsErrors: true`.
- Style and analysis enforced via StyleCop + Microsoft.NetAnalyzers.

## Release Process

1. Update `version` in `build.yaml` and `Directory.Build.props`
2. Update `changelog` in `build.yaml`
3. Run `dotnet publish --configuration Release`
4. Zip the DLL from `bin/Release/net9.0/publish/`
5. Update `manifest.json` with new version, checksum, and timestamp
6. Create a GitHub release with tag matching the version
7. Push the `develop` branch

## License

GPLv3. The binary plugin links against GPLv3 Jellyfin NuGet packages and is therefore covered by the GPLv3 license. Proprietary or source-unavailable distributions are not permitted.
