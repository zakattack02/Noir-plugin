# Jellyfin Plugin Template

Jellyfin plugin template targeting **net9.0** with C#. The actual plugin project lives under `Jellyfin.Plugin.Template/`.

## Build & Test

```bash
# Build solution
dotnet build Jellyfin.Plugin.Template.sln

# Publish (used for deployment to server)
dotnet publish Jellyfin.Plugin.Template.sln --configuration=Debug

# Or with full paths (for VS Code problem matcher):
dotnet build Jellyfin.Plugin.Template.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

There are **no unit tests** in this template. The CI test workflow (`test.yaml`) delegates to `jellyfin/jellyfin-meta-plugins`.

## Project structure

- `Jellyfin.Plugin.Template/Plugin.cs` — Main plugin class. Must inherit `BasePlugin<PluginConfiguration>` and implement `IHasWebPages` for config pages.
- `Jellyfin.Plugin.Template/Configuration/PluginConfiguration.cs` — Settings class. Inherits `BasePluginConfiguration`. Fields serialize to/from the dashboard config page.
- `Jellyfin.Plugin.Template/Configuration/configPage.html` — Embedded resource (dashboard config UI). Uses jQuery-free vanilla JS with `emby-*` web components.
- `Jellyfin.Plugin.Template/Jellyfin.Plugin.Template.csproj` — Targets `net9.0`. Key NuGet refs are `Jellyfin.Controller` and `Jellyfin.Model` with `<ExcludeAssets>runtime</ExcludeAssets>` (required — don't skip this, or the plugin won't register).

## Creating a new plugin from this template

1. Rename the solution, project, namespace, and `build.yaml` name/guid/artifacts.
2. Generate a **new GUID** for the plugin (both in `build.yaml` and `Plugin.cs`):
   ```bash
   uuidgen
   ```
3. Update `build.yaml`:
   - `name`, `guid`, `version`, `targetAbi`, `framework`, `category`, `owner`, `artifacts`
   - `targetAbi` must match the Jellyfin server version (e.g., `10.9.0.0`).
4. Update package versions in `.csproj` to match the target Jellyfin server version.
5. The `Version`/`AssemblyVersion`/`FileVersion` defaults to `0.0.0.0` via `Directory.Build.props` — override as needed.

## Plugin repository / manifest

- `build.yaml` is the metadata source. The CI pipeline (`publish.yaml`) delegates to `jellyfin/jellyfin-meta-plugins` which reads this file and generates a JSON manifest + publishes binaries.
- The JSON manifest format for plugin repositories (per jellyfin.org blog post) uses fields: `category`, `guid`, `name`, `description`, `owner`, `overview`, `versions[]` with `checksum`, `changelog`, `targetAbi`, `sourceUrl`, `timestamp`, `version`.
- The official manifest is hosted at `repo.jellyfin.org` via nginx; plugin binaries go on GitHub Releases.

## Key conventions

- **Style**: Jellyfin uses StyleCop + Microsoft.NetAnalyzers. Ruleset at `jellyfin.ruleset` — many SA rules disabled (SA1009, SA1101, SA1200, SA1309, SA1600 etc.). Select CA rules are errors (CA1305, CA1725, CA2016, CA2254).
- **Nullable**: enabled, `TreatWarningsAsErrors: true`, `AnalysisMode: AllEnabledByDefault`.
- **EditorConfig**: 4-space indent, `utf-8`, LF line endings. Instance fields prefixed with `_`, static fields prefixed with `_` too (not `s_` despite the comment). PascalCase for most members.
- **CI**: All workflows delegate to `jellyfin/jellyfin-meta-plugins` — build, test, publish, CodeQL, changelog, label sync. Renovate configured via `jellyfin/.github` preset.

## VS Code debugging

Pre-configured tasks in `.vscode/`:
- Expects `jellyfin` and `jellyfin-web` repos cloned as siblings.
- Edit `.vscode/settings.json` paths for your local setup:
  - `jellyfinDir` — path to `jellyfin/Jellyfin.Server`
  - `jellyfinWebDir` — path to `jellyfin-web`
  - `jellyfinLinuxDataDir` / `jellyfinWindowsDataDir` — server data dir
- `build-and-copy` task: publishes the plugin then `cp`-s the DLL to the server's plugin directory.
- Launch config runs `jellyfin.dll` with `--webdir` pointing at `jellyfin-web/dist/`.

## Licensing

The binary plugin **must** be GPLv3 (due to linking against GPLv3 NuGet packages). The template provides a GPLv3 LICENSE. Proprietary/source-unavailable plugins are not permitted for distribution.
