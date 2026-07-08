# REST API

All plugin endpoints are served under the Jellyfin base URL `<JellyfinUrl>/SpiderNoir/`. Endpoints that return static resources (`player-overlay.js`, `icon.svg`) are public (no authentication). All other endpoints require a valid Jellyfin user token in the `Authorization: MediaBrowser Token=<TOKEN>` header.

---

## `GET /SpiderNoir/versions/{itemId}`

Returns version information for a specific library item.

### Parameters

| Name   | In   | Type | Required | Description |
|--------|------|------|----------|-------------|
| `itemId` | path | GUID | yes | Jellyfin item identifier |

### Responses

- **200 OK** – Item found.
  ```json
  {
    "hasVersions": true,
    "itemName": "S01E01 - Episode Name",
    "seriesName": "SpiderNoir",
    "currentVersion": "color",
    "versions": [
      { "id": "color", "label": "Color", "path": "/media/SpiderNoir/S01E01.mkv" },
      { "id": "noir", "label": "Noir (B&W)", "path": "/media/SpiderNoir/S01E01 - Noir.mkv" }
    ]
  }
  ```
- **200 OK** – No alternate versions.
  ```json
  {
    "hasVersions": false,
    "currentVersion": "unknown",
    "versions": [],
    "message": "No alternate versions found for this item."
  }
  ```
- **404 Not Found** – Item does not exist.
  ```json
  { "error": "Item not found" }
  ```

### Example

```bash
curl -H "Authorization: MediaBrowser Token=YOUR_TOKEN" \
  "{JELLYFIN_URL}/SpiderNoir/versions/00000000-0000-0000-0000-000000000000"
```

---

## `POST /SpiderNoir/switch/{itemId}/{targetVersion}`

Switches the current version of an item to the specified target version and returns the path of the target file.

### Parameters

| Name          | In   | Type   | Required | Description |
|---------------|------|--------|----------|-------------|
| `itemId`      | path | GUID   | yes | Jellyfin item identifier |
| `targetVersion` | path | string | yes | Desired version – either `"noir"` or `"color"` |

### Responses

- **200 OK** – Switch successful.
  ```json
  {
    "success": true,
    "targetVersion": "noir",
    "targetPath": "/media/SpiderNoir/S01E01 - Noir.mkv",
    "itemId": "00000000-0000-0000-0000-000000000000"
  }
  ```
- **400 Bad Request** – Invalid `targetVersion` value.
  ```json
  { "error": "Invalid target version. Must be 'noir' or 'color'." }
  ```
- **404 Not Found** – Item not found.
  ```json
  { "error": "Item not found" }
  ```
- **404 Not Found** – No version pair for the item.
  ```json
  { "error": "No version pair found for this item." }
  ```
- **404 Not Found** – Requested target version not available for this item.
  ```json
  { "error": "Target version 'noir' not available for this item." }
  ```

### Example

```bash
curl -X POST -H "Authorization: MediaBrowser Token=YOUR_TOKEN" \
  "{JELLYFIN_URL}/SpiderNoir/switch/00000000-0000-0000-0000-000000000000/noir"
```

---

## `GET /SpiderNoir/series`

Lists all detected SpiderNoir series that contain dual‑version episodes.

### Responses

- **200 OK**
  ```json
  {
    "totalSeries": 1,
    "totalEpisodes": 12,
    "series": [
      {
        "seriesName": "SpiderNoir",
        "episodeCount": 12,
        "episodes": [
          {
            "episodeId": "00000000-0000-0000-0000-000000000000",
            "episodeName": "S01E01 - Episode",
            "hasNoir": true,
            "hasColor": true
          }
        ]
      }
    ]
  }
  ```

### Example

```bash
curl -H "Authorization: MediaBrowser Token=YOUR_TOKEN" \
  "{JELLYFIN_URL}/SpiderNoir/series"
```

---

## `GET /SpiderNoir/player-overlay.js`

Serves the JavaScript overlay that adds a version toggle UI to the Jellyfin web player. No authentication required.

### Responses

- **200 OK** – `Content-Type: application/javascript`

### Example

```bash
curl -O "{JELLYFIN_URL}/SpiderNoir/player-overlay.js"
```

---

## `GET /SpiderNoir/icon.svg`

Returns the plugin icon (DVD disc) SVG. No authentication required.

### Responses

- **200 OK** – `Content-Type: image/svg+xml`

### Example

```bash
curl -O "{JELLYFIN_URL}/SpiderNoir/icon.svg"
```

---

## Data Model: `EpisodeVersionPair`

```csharp
public class EpisodeVersionPair
{
    public Guid EpisodeId { get; set; }
    public string? EpisodeName { get; set; }
    public string? SeriesName { get; set; }
    public string? NoirPath { get; set; }
    public string? ColorPath { get; set; }
    public bool IsCurrentNoir { get; set; }
}
```

* `EpisodeId` – Unique identifier for the episode.
* `EpisodeName` – Display name of the episode.
* `SeriesName` – Name of the series the episode belongs to.
* `NoirPath` – File system path to the Noir (black‑and‑white) version, if present.
* `ColorPath` – File system path to the Color version, if present.
* `IsCurrentNoir` – Indicates whether the currently selected version is Noir.
