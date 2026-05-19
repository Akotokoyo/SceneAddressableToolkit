# SceneAddressableToolkit

Unity toolkit to describe a game zone with a `ScriptableObject` and Addressables references.

## Current status

At the moment, the project includes:

- `ZoneConfig`: runtime-ready zone data container.
- `ZoneEditorGeneration`: editor window to generate zone data from the current scene.

The goal is to centralize zone metadata and spawn content in a single asset for open-world and level-design workflows.

## Core data model

### ZoneConfig (`Assets/Scripts/ZoneConfig.cs`)

Main fields:

- `ZoneName`: readable zone name.
- `ZoneId`: numeric zone id.
- `MapReference`: Addressables reference for the main map.
- `SpawnEntries`: serialized list of objects to spawn in the zone.
- `sizeTier`: zone size classification (`NONE`, `SMALL`, `MEDIUM`, `BIG`).

### SpawnEntry

Each spawn entry contains:

- `Name`: logical entry name.
- `PrefabReference`: Addressables prefab reference to instantiate.
- `Position`, `Rotation`, `Scaling`: initial transform values.
- `Tag`, `LayerIndex`: gameplay/render metadata.
- `HasLodOverride`, `LodOverride`: optional LOD override.
- `HasCullingDistance`, `CullingDistance`: optional culling-distance override.

### ZoneSizeTier

Size enum:

- `NONE`: no classification.
- `SMALL`: small zone.
- `MEDIUM`: medium zone.
- `BIG`: large zone.

The `ZonSizeTierClassification` class includes helper thresholds to classify a zone by number of entries.

## Editor tooling

### ZoneEditorGeneration (`Assets/Scripts/ZoneEditorGeneration.cs`)

Open from Unity menu:

- `ToolKit/Generate Config Zone Form and To Scene`

Available controls:

- `Include Children`: include recursive traversal of child transforms.
- `Zone Name`: output config name.
- `Zone Id`: output numeric id.
- `Zone Config`: source config used to rebuild scene objects.

Buttons:

- `Generate Zone from Scene` (implemented)
- `Generate Scene from Zone` (implemented)
- `Recaulculate Size Tier` (implemented)

### Generate Zone from Scene behavior

Current implementation:

1. Reads the active loaded scene.
2. Asks where to save the generated `ZoneConfig` asset.
3. Creates a new `ZoneConfig` and fills `ZoneId` + `SpawnEntries`.
4. Scans either:
   - children of a root object named `World`, if present, or
   - all scene root objects otherwise.
5. Skips objects tagged `EditorOnly`.
6. Converts prefab instances into `SpawnEntry` records using:
   - prefab Addressables GUID (`AssetReference`)
   - world transform values
   - tag-based defaults (example: `Building` sets culling distance)
7. Saves the asset into the project.

Notes:

- Only prefab instances are collected.
- Non-addressable prefabs are currently logged with a warning.

### Generate Scene from Zone behavior

Current implementation:

1. Requires a valid `ZoneConfig` assigned in the editor window.
2. Validates that `SpawnEntries` is not empty.
3. Uses the active scene and ensures a `World` parent exists (creates it if missing).
4. Resolves each `SpawnEntry.PrefabReference` GUID to a prefab asset path.
5. Instantiates prefabs under `World` and applies:
   - `Position`, `Rotation`, `Scaling`
   - object `Name`
   - `LayerIndex` (if in valid Unity range `0..31`)
   - `Tag` (with warning if tag does not exist in project settings)
6. Registers operations in Unity Undo and marks the scene dirty.

Notes:

- Entries with unresolved/missing prefabs are skipped with warnings.
- The generation currently focuses on transforms + basic object metadata.

### Recaulculate Size Tier behavior

Current implementation:

1. Requires a valid `ZoneConfig` assigned in the editor window.
2. Validates that `SpawnEntries` is not empty.
3. Recomputes `sizeTier` using `ZonSizeTierClassification.GetSizeFromEntries(...)`.
4. Marks the `ZoneConfig` asset dirty and saves project assets.
5. Logs the resulting tier and entry count.

## Recommended workflow (current MVP)

1. Create a `ZoneConfig` asset (Create > ScriptableObject, if a dedicated menu exists in the project).
2. Set `ZoneName`, `ZoneId`, and `MapReference`.
3. (Optional) Use `ZoneEditorGeneration` to auto-generate `SpawnEntries` from the scene.
4. Set or compute `sizeTier` based on the number of elements.
5. Use the asset as a data source in zone streaming/loading systems.

## Known gaps / next steps

- The toolkit and documentation are still in an early stage.
- Useful next additions: runtime loading/unloading examples, validation pipeline, and naming conventions.
