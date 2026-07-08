# Installation

## Prerequisites

- Jellyfin server 10.9 or newer (matches `targetAbi: 10.9.0.0`).
- Jellyfin web client (required for the player overlay).

## Option 1: Add the plugin repository (recommended)

1. Open **Dashboard → Plugins → Repositories**.
2. Click **+ Add**.
3. Fill in:
   - **Name**: `SpiderNoir`
   - **URL**: `https://raw.githubusercontent.com/zakattack02/Noir-plugin/develop/manifest.json`
4. Press **Save**.
5. Switch to **Catalog**, locate **SpiderNoir**, and click **Install**.

## Option 2: Manual install (if you prefer a direct zip)

1. Download the latest zip from the [GitHub releases page](https://github.com/zakattack02/Noir-plugin/releases).
2. Extract the `Jellyfin.Plugin.SpiderNoir.dll` file.
3. Copy the DLL to Jellyfin’s plugin folder:
   - **Linux**: `~/.local/share/jellyfin/plugins/SpiderNoir/`
   - **Windows**: `%LOCALAPPDATA%\jellyfin\plugins\SpiderNoir\`
4. Restart Jellyfin to load the new plugin.

## Enable the player overlay (required for UI switching)

The overlay is delivered as an embedded JavaScript resource. Add its URL to Jellyfin’s **Custom JavaScript** field:

1. Open **Dashboard → General**.
2. Scroll to **Custom JavaScript**.
3. Paste the following line (exactly) and click **Save**:
   ```text
   /SpiderNoir/player-overlay.js
   ```

> **Note**: The overlay adds a DVD‑disc icon to the bottom‑right of the video player when a SpiderNoir episode with both versions is playing.

## Verify the installation

1. Go to **Dashboard → Plugins** – you should see **SpiderNoir** version `0.1.0.0` listed.
2. Play an episode that has both Noir and Color files.
3. The DVD icon should appear in the player's lower-right corner.
