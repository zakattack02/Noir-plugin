# SpiderNoir Plugin

**Quick links**: [Installation](installation.md) | [Configuration](configuration.md) | [Usage](usage.md) | [API](api.md) | [Development](development.md)

SpiderNoir is a Jellyfin plugin that detects episodes with both Black & White (Noir) and Color versions and lets you switch between them during playback.

## How it works

1. **Version Detection** — On library scan, the plugin finds SpiderNoir episodes that have sibling files for the other version (Noir or Color).
2. **API** — A REST API exposes the detected pairs and allows switching versions.
3. **Player Overlay** — A JavaScript overlay adds a DVD icon button to the video player. Click it to open a dropdown and switch versions.

## Naming conventions

The plugin supports two naming styles:

### Suffix-based
```
SpiderNoir S01E01.mkv              → Color (untagged = Color)
SpiderNoir S01E01 - Noir.mkv       → Noir version
SpiderNoir S01E01 - Color.mkv      → Explicit Color tag
```

### Folder-based
```
Series/Season 01/Color/S01E01.mkv
Series/Season 01/Noir/S01E01.mkv
```

## Architecture

```
┌─────────────┐     ┌──────────────────┐     ┌───────────────────┐
│ Plugin.cs   │────→│ SpiderNoirCtrl   │────→│ VersionDetection  │
│ (BasePlugin)│     │ (REST API)       │     │ Service           │
└─────────────┘     └──────────────────┘     └───────────────────┘
                           │                          │
                           ▼                          ▼
                    ┌──────────────┐          ┌──────────────┐
                    │ playerOverlay│          │ EpisodeVersion│
                    │ .js (Web)    │          │ Pair (Model)  │
                    └──────────────┘          └──────────────┘
```

## Project structure

```
Jellyfin.Plugin.SpiderNoir/
├── Api/
│   └── SpiderNoirController.cs       — REST API endpoints
├── Configuration/
│   ├── PluginConfiguration.cs        — Settings model
│   └── configPage.html               — Dashboard UI (embedded)
├── Services/
│   ├── VersionDetectionService.cs    — Scans for version pairs
│   ├── EpisodeVersionPair.cs         — Data model
│   └── PluginServiceRegistrator.cs   — DI registration
├── Web/
│   └── playerOverlay.js             — Player overlay (embedded)
├── Plugin.cs                         — Main plugin class
└── Jellyfin.Plugin.SpiderNoir.csproj — Project file (net9.0)
```
