# SpiderNoir Jellyfin Plugin

Jellyfin plugin that enables switching between Black & White (Noir) and Color versions of SpiderNoir series episodes from the video player.

Targets **net9.0** with C#. Project lives under `Jellyfin.Plugin.SpiderNoir/`.

## Build & Test

```bash
# Build solution
dotnet build Jellyfin.Plugin.SpiderNoir.sln

# Publish (for deployment to server's plugin directory)
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Debug

# With full paths for VS Code problem matcher:
dotnet build Jellyfin.Plugin.SpiderNoir.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

No unit tests in this template. CI delegates to `jellyfin/jellyfin-meta-plugins`.

## Project structure

- `Plugin.cs` — Main plugin class. Inherits `BasePlugin<PluginConfiguration>`, implements `IHasWebPages`. GUID: `3bd33ef7-dd55-485b-9487-8bce0b52bd55`.
- `Configuration/PluginConfiguration.cs` — Settings: naming convention (suffix/folder), Noir/Color suffixes, series name pattern, auto-detect toggle, player overlay toggle.
- `Configuration/configPage.html` — Dashboard config UI. Embedded resource using vanilla JS + `emby-*` web components. No jQuery.
- `Api/SpiderNoirController.cs` — REST API endpoints: `GET/POST /SpiderNoir/versions/{id}`, `POST /SpiderNoir/switch/{id}/{version}`, `GET /SpiderNoir/series`. All endpoints require auth.
- `Services/VersionDetectionService.cs` — Scans library for SpiderNoir episodes, detects Noir/Color sibling files using suffix or folder naming conventions.
- `Services/EpisodeVersionPair.cs` — Data model for detected version pairs.
- `Services/PluginServiceRegistrator.cs` — Registers `VersionDetectionService` as a DI singleton via `IPluginServiceRegistrator`.
- `Web/playerOverlay.js` — Client-side JS that adds a Noir/Color switcher overlay to the video player. Injects into Jellyfin web player DOM.
- `build.yaml` — Plugin metadata for CI/manifest generation.

## How the version detection works

**Suffix-based naming** (default):
- `SpiderNoir S01E01.mkv` — Color version (untagged = Color)
- `SpiderNoir S01E01 - Noir.mkv` — Noir version
- `SpiderNoir S01E01 - Color.mkv` — Explicit Color tag

**Folder-based naming**:
```
SpiderNoir/Season 01/Color/S01E01.mkv
SpiderNoir/Season 01/Noir/S01E01.mkv
```

The `playerOverlay.js` script auto-injects into the web player page, polls for SpiderNoir content, fetches versions from the API, and renders a toggle overlay.

## Creating a release / manifest

1. Update `build.yaml` version.
2. Tag the release in git; the CI workflow (`publish.yaml`) handles JSON manifest generation and binary publishing via `jellyfin/jellyfin-meta-plugins`.
3. The JSON manifest format uses fields: `category`, `guid`, `name`, `description`, `owner`, `overview`, `versions[]` with `checksum`, `changelog`, `targetAbi`, `sourceUrl`, `timestamp`, `version`.

## VS Code debugging

Pre-configured in `.vscode/`:
- Edit `.vscode/settings.json` to point `jellyfinDir`, `jellyfinWebDir`, `jellyfinLinuxDataDir` / `jellyfinWindowsDataDir` to your local Jellyfin server paths.
- `build-and-copy` task: publishes the plugin then copies the DLL to the server's plugin directory.
- Launch config runs `jellyfin.dll` with `--webdir` pointing at `jellyfin-web/dist/`.

## Key conventions

- **Style**: StyleCop + Microsoft.NetAnalyzers. Ruleset at `jellyfin.ruleset`. Many SA rules disabled (SA1009, SA1101, SA1200, SA1309, SA1600). Select CA rules are errors (CA1305, CA1725, CA2016, CA2254).
- **Nullable**: enabled. `TreatWarningsAsErrors: true`. `AnalysisMode: AllEnabledByDefault`.
- **EditorConfig**: 4-space indent, utf-8, LF endings. Instance fields prefixed with `_`. PascalCase for public members.
- **CI**: All workflows delegate to `jellyfin/jellyfin-meta-plugins`.
- **Renovate**: Configured via `jellyfin/.github` preset.

## Licensing

Binary plugin **must** be GPLv3 due to linking against GPLv3 Jellyfin NuGet packages. Proprietary/source-unavailable plugins not permitted for distribution.
