<div align="center">

<img src="icon.png" width="120" />

<br/>
<br/>

<img src="https://img.shields.io/badge/AutoCAD-2025%2B-E8210A?style=flat&logoColor=white" />
<img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white" />
<img src="https://img.shields.io/badge/License-MIT-green?style=flat" />

</div>

---

## What is it?

MatchX is a free AutoCAD plugin that extends `MATCHPROP` — match entity properties across layouts, between model space and paper space, with a live paint mode. Native `MATCHPROP` requires the source and destination to live in the same space; MatchX removes that limitation entirely.

---

## Commands

| Command | Description |
|---|---|
| `MX` | Pick a source entity, then select destinations by click or window. Enter to finish. |
| `MXRESET` | Clear the captured source and pick a new one. |
| `MXLIST` | Print the currently captured source — entity type, layer, and all captured properties. |

---

## Features

- Cross-layout and Model ↔ Paper Space property matching
- Paint mode — click or drag a window to select destinations, Enter to finish
- Live count as properties are applied
- Visual highlight on matched entities
- Deduplication — picking the same entity twice in a session is a no-op
- Source auto-clears when its document is closed
- Single undo step per paint session

---

## Supported properties

**Universal:** Color, Layer, Linetype, LinetypeScale, LineWeight, Transparency, PlotStyleName, Thickness

**Type-specific:**

| Entity | Properties |
|---|---|
| `TEXT` / `MTEXT` | TextStyle |
| `DIMENSION` | DimensionStyle |
| `HATCH` | PatternType, PatternName, PatternScale, PatternAngle, HatchStyle |
| `POLYLINE` | ConstantWidth |
| `MLEADER` | MLeaderStyle |

---

## Build

Requires AutoCAD 2025 installed at `C:\Program Files\Autodesk\AutoCAD 2025\` and the .NET 8 SDK.

dotnet build MatchX.csproj


Output: `bin\Debug\net8.0-windows\MatchX.dll`

---

## Load into AutoCAD

1. Open AutoCAD 2025 or 2026
2. Run `NETLOAD`
3. Browse to `MatchX.dll`
4. Command line confirms `MatchX loaded. Run MX to begin.`

---

## Roadmap

- [x] Cross-layout and Model ↔ Paper Space matching
- [x] Paint mode with click and window selection
- [x] Live count and entity highlighting
- [x] Deduplication
- [x] MXLIST — inspect captured source
- [x] Auto-clear on document close
- [ ] MXS settings — toggle which properties transfer
- [ ] Cross-drawing support
- [ ] Persistent settings
- [ ] Autodesk App Store listing

---

## Issues

Found a bug or have a suggestion? [Open an issue](https://github.com/nuageuk/MatchX/issues), all feedback welcome.

---

*Built by [nuageuk](https://github.com/nuageuk)*