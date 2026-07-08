# Development

## Build

```bash
# Build solution
dotnet build Jellyfin.Plugin.SpiderNoir.sln

# Build with full paths (for VS Code problem matcher)
dotnet build Jellyfin.Plugin.SpiderNoir.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

# Publish release
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Release

# Publish debug
dotnet publish Jellyfin.Plugin.SpiderNoir.sln --configuration Debug
```

No unit tests exist in this template. CI delegates to `jellyfin/jellyfin-meta-plugins`.

## Project structure

```
Jellyfin.Plugin.SpiderNoir/
├── Api/
│   └── SpiderNoirController.cs       — REST API (versions, switch, series, icon, player-overlay.js)
├── Configuration/
│   ├── PluginConfiguration.cs        — Settings model with defaults
│   └── configPage.html               — Embedded dashboard config page (vanilla JS, no jQuery)
├── Services/
│   ├── VersionDetectionService.cs    — Scans library for Noir/Color pairs
│   ├── EpisodeVersionPair.cs         — Data model
│   └── PluginServiceRegistrator.cs   — DI singleton registration
├── Web/
│   └── playerOverlay.js             — Player overlay script (embedded resource)
├── Plugin.cs                         — BasePlugin<PluginConfiguration>, IHasWebPages
├── Jellyfin.Plugin.SpiderNoir.csproj — net9.0, TreatWarningsAsErrors, nullable
├── build.yaml                        — Plugin metadata for CI
└── manifest.json                     — Plugin repo manifest
```

## Key conventions

- **StyleCop + Microsoft.NetAnalyzers** — ruleset at `jellyfin.ruleset`
- **Nullable**: enabled, `TreatWarningsAsErrors: true`, `AnalysisMode: AllEnabledByDefault`
- **EditorConfig**: 4-space indent, utf-8, LF endings
- **Naming**: Instance fields prefixed with `_`, PascalCase for public members
- **CI**: All workflows delegate to `jellyfin/jellyfin-meta-plugins`

## Adding a new API endpoint

1. Add the route in `Api/SpiderNoirController.cs`
2. If it returns data, use `[HttpGet]` or `[HttpPost]` attributes
3. For endpoints returning embedded resources, use `Assembly.GetExecutingAssembly().GetManifestResourceStream()`
4. Register any new embedded resources in the `.csproj`

## Adding new settings

1. Add the property to `Configuration/PluginConfiguration.cs` with a default value
2. Add the UI control to `Configuration/configPage.html`
3. Add load/save logic in the config page's JavaScript

## VS Code debugging

Pre-configured in `.vscode/`:

```jsonc
// .vscode/settings.json — edit to match your paths
{
    "jellyfinDir": "${workspaceFolder}/../jellyfin/Jellyfin.Server",
    "jellyfinWebDir": "${workspaceFolder}/../jellyfin-web",
    "jellyfinLinuxDataDir": "$HOME/.local/share/jellyfin",
    "pluginName": "Jellyfin.Plugin.SpiderNoir"
}
```

- Launch config runs `jellyfin.dll` with `--webdir`
- `build-and-copy` task: builds, creates plugin dir, copies DLL

## Creating a release

1. Update `version` in `build.yaml` and `Directory.Build.props`
2. Update `changelog` in `build.yaml`
3. Build Release: `dotnet publish --configuration Release`
4. Create zip of the DLL from `bin/Release/net9.0/publish/`
5. Update `manifest.json` with new version, checksum, timestamp
6. Create GitHub release with tag matching the version
7. Push `develop` branch

## Embedded resources

Resources loaded by the web client must be registered in `.csproj`:

```xml
<EmbeddedResource Include="Web\playerOverlay.js" />
<EmbeddedResource Include="..\dvd-disk.svg" Link="Web\dvd-disk.svg" />
```

Access them from the controller using the resource name format: `Jellyfin.Plugin.SpiderNoir.{Folder}.{Filename}`

## Licensing

The binary plugin **must** be GPLv3 due to linking against GPLv3 Jellyfin NuGet packages. Proprietary/source-unavailable plugins are not permitted for distribution.
