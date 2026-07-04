# Death Anchor Level Editor

This is the HTML greybox level editor for the Death Anchor prototype.

## Canvas And Asset Units

- Editor viewport / fixed camera frame: `1024 x 576`.
- Default grid and tile unit: `32 x 32`.
- Default placeable sizes match the current art list: terrain/spike/button/bridge/key/laser emitter `32 x 32`, door `32 x 64`, moving platform `96 x 32`, exit/death anchor `64 x 64`, player `32 x 32`.
- Terrain and bridge rectangles can still be resized in the editor, but they should be treated as 32 px tile strips when handed to art/programming.
- Saved jam levels are kept in `Tools/DeathAnchorLevelEditor/level`.
- Moving platforms support `motionMode: "button"` and `motionMode: "auto"`.
- Automatic moving platforms ignore button links and cannot be paired with buttons.
- Lasers can set `attachedTo` to a moving platform id; in editor playtest they follow that platform.

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
- Movement values mirror `Assets/Scripts/DeathAnchor/DeathAnchorPlayerController.cs`; player bounds are set to the 32 px art canvas.
- Coordinate conversion follows `Assets/Editor/DeathAnchor/DeathAnchorLevelBaker.cs`: `100 px = 1 Unity unit`.
- Exported level JSON also includes `player.physics` for handoff.
