# Configuration

Access the configuration page via **Dashboard → Plugins → SpiderNoir**.

## Settings

| Setting | Type | Default | Description |
|---|---|---|---|
| **Version Naming Convention** | `VersionNamingConvention` (enum) | `SuffixBased` | Determines how the plugin pairs Noir/Color episodes. Options: `SuffixBased`, `FolderBased`, `Custom`.
| **Noir (B&W) Suffix** | `string` | `"Noir"` | File suffix that marks the Black‑&‑White version. Also recognized: `"B&W"`.
| **Color Suffix** | `string` | `"Color"` | File suffix that marks the Color version.
| **Series Name Pattern** | `string` | `"SpiderNoir"` | Sub‑string matched against series titles to auto‑detect SpiderNoir series.
| **Auto‑Detect SpiderNoir Series** | `bool` | `true` | When enabled, the plugin scans all series whose name contains the **Series Name Pattern** and attempts version detection.
| **Enable Player Overlay** | `bool` | `true` | Controls whether the DVD‑icon overlay is injected into the web player. Must also add the custom JavaScript URL (see Installation).

## Naming conventions

### Suffix‑based (default)

Untagged files are treated as the **Color** version. Add a suffix for the alternate version:

```
SpiderNoir S01E01.mkv                 → Color (default)
SpiderNoir S01E01 - Noir.mkv          → Noir (B&W)
SpiderNoir S01E01 - Color.mkv         → Explicit Color tag
SpiderNoir S01E01 - B&W.mkv           → Noir (alternative tag)
```

You can change the suffix strings in the settings above.

### Folder‑based

Episodes are stored in separate subfolders under the series directory:

```
SpiderNoir/Season 01/Color/S01E01.mkv
SpiderNoir/Season 01/Noir/S01E01.mkv
SpiderNoir/Season 01/B&W/S01E01.mkv   (alternative Noir folder)
```

The plugin detects the counterpart by looking for the same filename in the opposite folder.

## Auto‑detection workflow

When **Auto‑Detect SpiderNoir Series** is on, the plugin:
1. Retrieves all series whose title contains the **Series Name Pattern** (case‑insensitive).
2. Scans each episode in those series using the selected naming convention.
3. Builds a list of `EpisodeVersionPair` objects for episodes that have both Noir and Color files.

These pairs are exposed via the REST API and used by the player overlay to present the toggle UI.
