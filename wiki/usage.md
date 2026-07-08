# Usage

## Player overlay

Once installed and configured, the plugin adds a version switcher to the Jellyfin video player. The overlay is provided by the embedded JavaScript resource `player-overlay.js`. After adding its URL to **Dashboard → General → Custom JavaScript**, the UI appears automatically.

### UI layout

The overlay injects a **DVD‑disc icon** in the bottom‑right corner of the player. Clicking the icon opens a **dropdown menu** that shows the two available versions:

- **Noir (B&W)** – displayed with a checkmark (✓) when currently selected.
- **Color** – displayed similarly when active.

```
           ┌─────────┐
           │ VERSION │   ← dropdown header
           │ ✓ Noir  │   ← active version
           │   Color │   ← alternate version
           └─────────┘
                 [>>]   ← DVD‑icon toggle button
```

### Switching versions

1. Play an episode that has both Noir and Color files.
2. Click the **DVD icon** → the dropdown appears.
3. Select the desired version.
4. The player reloads the selected file while preserving the current playback position.
5. The dropdown closes automatically.

### When the overlay is shown

- The plugin is installed and enabled.
- **Custom JavaScript** URL (`/SpiderNoir/player-overlay.js`) has been added.
- The currently playing item belongs to a series whose name matches the **Series Name Pattern** (default `SpiderNoir`).
- Both Noir and Color versions are detected for that episode.

### Troubleshooting

| Problem | Likely cause | Fix |
|---|---|---|
| No DVD icon in player | Custom JS not added | Add `/SpiderNoir/player-overlay.js` in Dashboard → General → Custom JavaScript |
| Icon shows but no menu | No version pairs detected | Verify naming convention matches your file layout |
| "Failed to switch version" | Alternate file not found | Ensure both Noir and Color files exist on disk and follow the naming pattern |
| Plugin not in catalog | Manifest URL not added | Add the raw manifest URL to Dashboard → Plugins → Repositories |


