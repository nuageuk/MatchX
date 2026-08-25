# MatchX

An AutoCAD plugin prototype that works around the native `MATCHPROP` limitation
where the source and destination objects must live in the same space (model
space or the same paper space layout). MatchX lets you pick a source entity in
one layout and apply its properties to entities in a different layout.

## How it works

- `MX` (first run): pick a source entity. Its `ObjectId` and owning space are
  cached in memory.
- `MX` (subsequent runs): select target entities.
  - If the targets are in the same space as the source, MatchX builds an
    implied (pickfirst) selection of `[source, ...targets]` and runs the
    native `MATCHPROP` command directly.
  - If the targets are in a different space, MatchX deep-clones the source
    entity into the target space, uses the clone as the pickfirst source for
    `MATCHPROP`, and erases the clone once the command finishes.
- `MXRESET`: clears the cached source so a new one can be picked.

## Build

Requires the .NET Framework 4.8 targeting pack and AutoCAD 2024 installed at
`C:\Program Files\Autodesk\AutoCAD 2024\` (the project references
`accoremgd.dll`, `acdbmgd.dll`, and `acmgd.dll` from that folder).

The project targets `net48`, which matches AutoCAD 2024's managed API
assemblies. AutoCAD 2025+ moved its managed API to .NET 8, so if you are
building against AutoCAD 2025 or later instead, point the `HintPath`
entries in `MatchX.csproj` at that install folder and retarget the project
to `net8.0-windows`.

```
dotnet build MatchX.csproj
```

The output DLL is written to `bin\Debug\net48\MatchX.dll` (or
`bin\Release\net48\MatchX.dll` with `-c Release`).

## Load into AutoCAD

1. Start AutoCAD 2024.
2. Run the `NETLOAD` command.
3. Browse to and select `MatchX.dll` from the build output folder.
4. The status bar / command line should print
   `MatchX loaded. Run MX to begin.`

## Test procedures

### Test 1 - same-space match (native path)

1. In model space, draw two entities on different layers (e.g. two circles).
2. Run `MX`, pick the first circle as the source.
3. Run `MX` again, select the second circle as the target.
4. Confirm the target's layer/color/linetype now match the source, the same
   way native `MATCHPROP` would behave.

### Test 2 - cross-layout match (clone fallback)

1. Draw an entity in model space with a distinct layer/color.
2. Switch to a paper space layout and draw a plain entity there.
3. Run `MX` while in model space and pick the model space entity as source.
4. Switch to the paper space layout, run `MX`, and select the paper space
   entity as the target.
5. Confirm the paper space entity picks up the source's properties, and that
   no leftover clone entity remains in the paper space layout afterward
   (check with a quick `SELECT ALL` or by inspecting the layout).

### Test 3 - reset and re-pick

1. After completing Test 1 or Test 2, run `MXRESET`.
2. Confirm the command line reports the source was cleared.
3. Run `MX` and verify it prompts to pick a new source entity rather than
   asking for targets.
