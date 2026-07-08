# SpiderNoir Jellyfin Plugin

Switch between Black & White (Noir) and Color versions of SpiderNoir series episodes directly from the Jellyfin video player.

**Version:** 0.2.0.0 | **Target:** Jellyfin 10.9+ (ABI 10.9.0.0) | **Framework:** .NET 9.0 | **License:** GPLv3

## Features

- **Automatic Version Detection** — Scans your library for episodes with both Noir and Color versions using suffix-based or folder-based naming conventions.
- **Player Overlay** — A DVD icon toggle button in the video player opens a dropdown menu to switch versions with a single click. Playback resumes at the same position.
- **REST API** — Query available versions, switch between them, and list detected series. All data endpoints require authentication.
- **Dashboard Configuration** — Configure naming conventions, auto-detection, and overlay behavior from the Jellyfin dashboard at **Dashboard -> Plugins -> SpiderNoir**.

## Installation

### Option 1: Plugin Repository (Recommended)

1. Go to **Dashboard -> Plugins -> Repositories**
2. Click **+ Add**
3. Enter:
   - **Repository Name**: `SpiderNoir`
   - **Repository URL**: `https://raw.githubusercontent.com/zakattack02/Noir-plugin/develop/manifest.json`
4. Click **Save**
5. Go to **Catalog**, find **SpiderNoir**, click **Install**
6. Restart Jellyfin

### Option 2: Manual Install

1. Download the latest `.zip` from [GitHub Releases](https://github.com/zakattack02/Noir-plugin/releases)
2. Extract `Jellyfin.Plugin.SpiderNoir.dll`
3. Create a `SpiderNoir` folder in your Jellyfin plugins directory:
   - **Linux**: `~/.local/share/jellyfin/plugins/SpiderNoir/`
   - **Windows**: `%LOCALAPPDATA%/jellyfin/plugins/SpiderNoir/`
4. Copy `Jellyfin.Plugin.SpiderNoir.dll` into that folder
5. Restart Jellyfin

### Enable the Player Overlay

The overlay script is served by the plugin. To activate it:

1. Go to **Dashboard -> General**
2. Scroll to the **Custom JavaScript** section
3. Paste: `/SpiderNoir/player-overlay.js`
4. Click **Save**

After configuring, a DVD icon will appear in the bottom-right of the video player when playing SpiderNoir content that has both Noir and Color versions detected. See [Usage](wiki/usage.md) for details.

## Naming Conventions

Two naming conventions are supported. Configure which one to use in the plugin settings.

### Suffix-Based (Default)

Untagged files are treated as the Color version. Add a suffix for the alternate version:

```
SpiderNoir S01E01.mkv                   Color version (untagged = Color)
SpiderNoir S01E01 - Noir.mkv            Noir (B&W) version
SpiderNoir S01E01 - Color.mkv           Explicit Color tag
SpiderNoir S01E01 - B&W.mkv             Alternate Noir tag (also detected)
SpiderNoir S01E01 B&W.mkv               Space-separated alternate tag
```

The detection logic tries ` - ` (dash-space) separator first, then ` ` (space-only) as fallback.

### Folder-Based

Episodes are stored in separate `Color/` and `Noir/` subdirectories under the season folder:

```
SpiderNoir/Season 01/Color/S01E01.mkv
SpiderNoir/Season 01/Noir/S01E01.mkv
```

The service also checks for a `B&W/` folder as an alternative to `Noir/`.

## Configuration

Access the configuration page at **Dashboard -> Plugins -> SpiderNoir**.

| Setting | Type | Default | Description |
|---|---|---|---|
| Version Naming Convention | enum | `SuffixBased` | How to pair Noir/Color episodes (`SuffixBased`, `FolderBased`, `Custom`) |
| Noir (B&W) Suffix | string | `Noir` | File suffix that identifies the Black & White version |
| Color Suffix | string | `Color` | File suffix that identifies the Color version |
| Series Name Pattern | string | `SpiderNoir` | Series name substring to match for auto-detection |
| Auto-Detect SpiderNoir Series | bool | `true` | Automatically scan for series matching the name pattern |
| Enable Player Overlay | bool | `true` | Show the version switch overlay in the video player |

## API Endpoints

All endpoints are under `/{JellyfinBaseUrl}/SpiderNoir/`. Endpoints serving static resources (`player-overlay.js`, `icon.svg`) are public (`[AllowAnonymous]`). All data endpoints require Jellyfin authentication (`[Authorize]`).

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/SpiderNoir/versions/{itemId}` | Yes | Get version information (Color/Noir paths, current version) for a library item |
| `POST` | `/SpiderNoir/switch/{itemId}/{targetVersion}` | Yes | Resolve the file path for a target version (`noir` or `color`) |
| `GET` | `/SpiderNoir/series` | Yes | List all detected SpiderNoir series with episode version pair counts |
| `GET` | `/SpiderNoir/player-overlay.js` | No | Serve the player overlay JavaScript for Custom JS injection |
| `GET` | `/SpiderNoir/icon.svg` | No | Serve the plugin's DVD disk icon |

### `GET /SpiderNoir/versions/{itemId}`

Returns the available versions for a given episode:

```json
{
  "hasVersions": true,
  "itemName": "S01E01 - Pilot",
  "seriesName": "SpiderNoir",
  "currentVersion": "color",
  "versions": [
    { "id": "color", "label": "Color", "path": "/path/to/S01E01.mkv" },
    { "id": "noir", "label": "Noir (B&W)", "path": "/path/to/S01E01 - Noir.mkv" }
  ]
}
```

### `POST /SpiderNoir/switch/{itemId}/{targetVersion}`

Returns the target file path for switching. `targetVersion` must be `noir` or `color`.

```json
{
  "success": true,
  "targetVersion": "noir",
  "targetPath": "/path/to/S01E01 - Noir.mkv",
  "itemId": "..."
}
```

### `GET /SpiderNoir/series`

Returns all detected series grouped with episode counts:

```json
{
  "totalSeries": 1,
  "totalEpisodes": 12,
  "series": [
    {
      "seriesName": "SpiderNoir",
      "episodeCount": 12,
      "episodes": [
        { "episodeId": "...", "episodeName": "Pilot", "hasNoir": true, "hasColor": true }
      ]
    }
  ]
}
```

## Project Structure

```
Jellyfin.Plugin.SpiderNoir/
├── Api/
│   └── SpiderNoirController.cs       REST API (5 endpoints, authenticated + public)
├── Configuration/
│   ├── PluginConfiguration.cs        Settings model with defaults
│   └── configPage.html               Embedded dashboard config page (vanilla JS)
├── Services/
│   ├── VersionDetectionService.cs    Library scanner for Noir/Color pairs
│   ├── EpisodeVersionPair.cs         Data model for version pairs
│   └── PluginServiceRegistrator.cs   DI singleton registration
├── Web/
│   └── playerOverlay.js              Client-side overlay script (embedded resource)
├── Plugin.cs                         Main plugin class, BasePlugin, IHasWebPages
├── Jellyfin.Plugin.SpiderNoir.csproj Project file
├── build.yaml                        CI/metadata for manifest generation
├── manifest.json                     Plugin repository manifest
```

Other root-level files:

```
├── .vscode/                          VS Code launch + task configs for debugging
├── wiki/                             Documentation pages
├── Directory.Build.props             Shared version info (0.2.0.0)
├── jellyfin.ruleset                  StyleCop + NetAnalyzers ruleset
├── dvd-disk.svg                      Plugin icon SVG
```

## Development Setup

### Prerequisites

- .NET 9.0 SDK
- A local clone of [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin) built at least once
- (Optional) A local clone of [jellyfin/jellyfin-web](https://github.com/jellyfin/jellyfin-web) built at least once

### VS Code Quick Start

Pre-configured tasks and launch configs are in `.vscode/`.

1. Edit `.vscode/settings.json` to point to your local Jellyfin paths:
   ```jsonc
   {
     "jellyfinDir": "${workspaceFolder}/../jellyfin/Jellyfin.Server",
     "jellyfinWebDir": "${workspaceFolder}/../jellyfin-web",
     "jellyfinLinuxDataDir": "$HOME/.local/share/jellyfin",
     "jellyfinWindowsDataDir": "${env:LOCALAPPDATA}/jellyfin",
     "pluginName": "Jellyfin.Plugin.SpiderNoir"
   }
   ```
2. Press **F5** (Launch configuration). This runs the `build-and-copy` task, which:
   - Publishes the plugin (`dotnet publish --configuration=Debug`)
   - Creates the plugin directory in your Jellyfin data directory
   - Copies the DLL to the plugin directory
   - Then launches Jellyfin from your local clone with `--webdir` pointing at your jellyfin-web build

### Build from Source

```bash
# Build the solution
dotnet build Jellyfin.Plugin.SpiderNoir.sln

# Build with full paths (for VS Code problem matcher)
dotnet build Jellyfin.Plugin.SpiderNoir.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

# Publish Release (output in bin/Release/net9.0/publish/)
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Release

# Publish Debug
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Debug
```

The single DLL artifact is `Jellyfin.Plugin.SpiderNoir.dll`.

### Code Conventions

- **Nullable**: enabled, `TreatWarningsAsErrors: true`, `AnalysisMode: AllEnabledByDefault`
- **Style**: StyleCop + Microsoft.NetAnalyzers via `jellyfin.ruleset`. Many SA rules disabled (SA1009, SA1101, SA1200, SA1309, SA1600). Select CA rules are errors (CA1305, CA1725, CA2016, CA2254).
- **EditorConfig**: 4-space indent, UTF-8, LF line endings
- **Naming**: instance fields prefixed with `_`, PascalCase for public members
- **CI**: workflows delegate to `jellyfin/jellyfin-meta-plugins`
- **No unit tests** in this template. CI relies on `jellyfin/jellyfin-meta-plugins`.

### Release Process

1. Update `version` in `build.yaml` and `Directory.Build.props`
2. Update `changelog` in `build.yaml`
3. Run `dotnet publish --configuration Release`
4. Create a `.zip` of `Jellyfin.Plugin.SpiderNoir.dll` from `bin/Release/net9.0/publish/`
5. Update `manifest.json` with the new version, checksum (uppercase hex), and timestamp
6. Create a GitHub release with a tag matching the version
7. Push the `develop` branch to trigger CI manifest generation

Manifest format fields: `category`, `guid`, `name`, `description`, `owner`, `overview`, `versions[]` with `checksum`, `changelog`, `targetAbi`, `sourceUrl`, `timestamp`, `version`, `dependencies`.

## License

GPLv3. The binary plugin links against GPLv3 Jellyfin NuGet packages and is therefore covered by the GNU General Public License v3. Proprietary or source-unavailable distributions are not permitted. See [`LICENSE`](LICENSE) for the full license text.
