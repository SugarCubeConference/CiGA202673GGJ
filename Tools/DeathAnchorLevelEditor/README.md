# Death Anchor Level Editor

This is the HTML greybox level editor for the Death Anchor prototype.

## Run

Open `index.html` directly, or serve the Unity project root with a local static server:

```powershell
cd D:\游戏开发\2026CGJ\project
python -m http.server 8770
```

Then open:

```text
http://127.0.0.1:8770/Tools/DeathAnchorLevelEditor/
```

## Shared Player Physics

`Assets/StreamingAssets/DeathAnchor/player-physics.json` is the Unity-side shared movement tuning file. `Tools/DeathAnchorLevelEditor/player-physics.json` is a local fallback for serving only the tool folder.

- The editor reads the `editor` values when served over HTTP.
- Unity should use the `unity` values.
- Current values mirror `Assets/Scripts/DeathAnchor/DeathAnchorPlayerController.cs`.
- Coordinate conversion follows `Assets/Editor/DeathAnchor/DeathAnchorLevelBaker.cs`: `100 px = 1 Unity unit`.
- Exported level JSON also includes `player.physics` for handoff.
