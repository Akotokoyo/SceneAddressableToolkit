# SceneAddressableToolkit

![Unity](https://img.shields.io/badge/Unity-2022.3.58f1-black?logo=unity)
![Addressables](https://img.shields.io/badge/Addressables-1.22.3-blue)
![License](https://img.shields.io/badge/license-MIT-green)

A Unity editor + runtime toolkit for open-world and level-design pipelines.  
Convert a scene into a data-driven `ZoneConfig` asset (and back), then stream it at runtime with **prioritized chunk loading** and **distance-based culling**.

---

## Overview

Managing large numbers of objects in open-world scenes is painful — prefabs scattered across the hierarchy, no easy way to version or stream them, and LOD handled ad-hoc.

**SceneAddressableToolkit** solves this by:

1. **Baking** a scene into a `ZoneConfig` ScriptableObject: one asset that describes every Addressable prefab in the zone (position, rotation, scale, culling, LOD metadata).
2. **Restoring** a scene from a `ZoneConfig` at edit time, with full Undo support.
3. **Streaming** the zone at runtime — critical objects near the player load first, the rest follow.
4. **Culling** spawned objects by distance with hysteresis so they activate/deactivate smoothly without flickering.

---

## Requirements

| Dependency | Version |
|------------|---------|
| Unity | 2022.3.58f1 (LTS) |
| Addressables | 1.22.3 |

All prefabs intended for use with this toolkit must be registered as **Addressable assets**.

---

## Installation

1. Clone or download this repository into your Unity project root.
2. Open the project in Unity 2022.3.58f1.
3. Ensure `com.unity.addressables 1.22.3` is present in `Packages/manifest.json` (it is, if you cloned this repo).
4. Open `Window > Asset Management > Addressables > Groups` and confirm your prefabs are registered.

---

## Quick start

### Scene → ZoneConfig (bake)

1. Open the scene you want to bake.
2. Go to `ToolKit > Generate Config Zone Form and To Scene`.
3. Fill in **Zone Name** and **Zone Id**.
4. Click **Generate Zone from Scene** and pick a save path.
5. Done — a `.asset` file now describes your entire zone.

### ZoneConfig → Scene (restore)

1. Open the target scene.
2. Open the same toolkit window and assign the `.asset` to the **Zone Config** field.
3. Click **Generate Scene from Zone**.
4. All prefabs are instantiated under a `World` root. Use **Ctrl+Z** to undo everything in one step.

### Runtime streaming

1. Add `SpawnerManager` and `ZoneLODManager` to a GameObject.
2. Assign the `ZoneConfig` asset and the player `Transform` to `SpawnerManager`.
3. Press Play — objects near the player load first, distant objects follow, culling is automatic.

---

## Architecture

```
Assets/Scripts/
├── Zone/
│   ├── ZoneConfig.cs          # ScriptableObject — zone data container
│   ├── ZoneEditorGeneration.cs # Editor window (bake / restore / recalculate tier)
│   └── ZoneChunkUtility.cs    # Static helpers: WorldToChunkXZ, ChebyshevDistance
└── Managers/
    ├── SpawnerManager.cs      # Runtime loader with chunk prioritization
    └── ZoneLODManager.cs      # Distance culling with hysteresis
```

---

## Data model

### ZoneConfig

| Field | Type | Description |
|-------|------|-------------|
| `ZoneName` | `string` | Readable zone name. |
| `ZoneId` | `int` | Numeric zone identifier. |
| `MapReference` | `AssetReference` | Addressables reference to the main map asset. |
| `SpawnEntries` | `List<SpawnEntry>` | All objects in the zone. |
| `sizeTier` | `ZoneSizeTier` | Auto-computed size class (`NONE` / `SMALL` / `MEDIUM` / `BIG`). |
| `ChunkSizeWorld` | `float` | World-space chunk cell size (XZ). `<= 0` disables chunk partitioning. |
| `ChunkGridOrigin` | `Vector3` | World-space origin of the chunk grid. |
| `InitialLoadChunkRing` | `int` | Chebyshev ring around the player chunk loaded in the critical wave. `2` = 5×5 chunks. |

### SpawnEntry

| Field | Type | Description |
|-------|------|-------------|
| `Name` | `string` | Object name. |
| `PrefabReference` | `AssetReference` | Addressables prefab reference. |
| `Position` / `Rotation` / `Scaling` | `Vector3` | World transform. |
| `Tag` / `LayerIndex` | `string` / `int` | Gameplay and render metadata. |
| `HasLodOverride` / `LodOverride` | `bool` / `int` | Optional LOD level override. |
| `HasCullingDistance` / `CullingDistance` | `bool` / `float` | Optional per-object culling distance. |

### ZoneSizeTier

| Value | Entry count |
|-------|-------------|
| `NONE` | Unclassified |
| `SMALL` | < 1 000 |
| `MEDIUM` | < 10 000 |
| `BIG` | ≥ 10 000 |

---

## Editor tooling

### ZoneEditorGeneration

Menu path: `ToolKit > Generate Config Zone Form and To Scene`

#### Generate Zone from Scene
- Scans children of a `World` root object, or all scene roots if none exists.
- Skips `EditorOnly`-tagged objects.
- Only collects prefab instances with a valid Addressables entry (others are warned).
- Applies tag-based culling defaults (e.g. `Building` → culling distance 180 u).
- Computes and writes `sizeTier` automatically.

#### Generate Scene from Zone
- Instantiates all `SpawnEntry` prefabs under a `World` parent.
- Applies transform, name, layer, and tag per entry.
- Fully undoable with a single Ctrl+Z.

#### Recalculate Size Tier
- Recomputes `sizeTier` on the assigned `ZoneConfig` from the current entry count.

---

## Runtime tooling

### SpawnerManager

| Inspector field | Description |
|-----------------|-------------|
| `zoneConfig` | Zone to load on `Start`. Overridable via `LoadZoneConfig(config)`. |
| `zoneLODManager` | Auto-resolved via `FindObjectOfType` if not assigned. |
| `playerTransform` | Determines player chunk and load-order sort origin. |
| `maxConcurrentAddressableLoads` | Max parallel Addressables operations. `0` = unlimited (wave order still respected). |

**Load flow:**

```
playerTransform == null  →  flat load, sorted from world origin (0,0,0)
ChunkSizeWorld <= 0      →  flat load, sorted from player position
otherwise                →  staged load:
                              1. critical wave  (chunks within InitialLoadChunkRing)
                              2. deferred wave  (everything else)
                            → each wave sorted by distance, limited by maxConcurrentAddressableLoads
                            → deferred starts only after critical is fully complete
```

After each successful spawn, the instance is tracked internally and registered with `ZoneLODManager`.

**Public API:**

```csharp
// Load a zone at runtime (also called automatically on Start).
spawnerManager.LoadZoneConfig(zoneConfig);

// Release all spawned Addressable instances and clear LOD tracking.
// Call this before loading the next zone (e.g. on door/portal transition).
spawnerManager.UnloadZone();
```

### ZoneLODManager

| Inspector field | Description |
|-----------------|-------------|
| `playerTransform` | Auto-resolved from `GameObject.FindGameObjectWithTag("Player")`. |
| `updateInterval` | Seconds between culling passes (default `0.3`). |
| `reactivateDistanceMultiplier` | Fraction of culling distance at which a culled object reactivates (default `0.9`). |

**Culling rules (per tracked object, evaluated in squared-distance space):**

- Deactivated when `distance > CullingDistance`.
- Reactivated when `distance < CullingDistance × reactivateDistanceMultiplier`.
- Objects with `HasCullingDistance = false` or `CullingDistance <= 0` are never culled.
- Destroyed instances are automatically pruned from the tracking list.

**Public API:**

```csharp
// Called automatically by SpawnerManager after each successful spawn.
lodManager.RegisterSpawnedObject(gameObject, spawnEntry);

// Remove a single object (e.g. on manual destroy).
lodManager.UnRegisterSpawnedObject(gameObject);

// Clear all tracked objects (e.g. on zone unload).
lodManager.ClearTrackedObjects();
```

---

## Design notes

### LOD via positional objects

The toolkit does not use Unity's built-in `LODGroup` component. Instead, LOD variants are treated as **separate `SpawnEntry` records placed at different world positions**.

This was an intentional choice: the reference scene used during development contained purchased 3D assets whose licenses do not allow redistribution. To keep the repository self-contained and publicly shareable, LOD meshes could not be bundled — so the LOD system was designed around distance-based activation/deactivation of independent objects rather than mesh-level LOD switching.

The `HasLodOverride` / `LodOverride` fields on `SpawnEntry` are reserved for future per-entry LOD group control once suitable free assets are available.

---

## Limitations / roadmap

- `HasLodOverride` / `LodOverride` runtime application is not yet implemented (see design note above).
- Chunk loading is one-shot at scene start; dynamic streaming (load/unload as the player moves) is not yet implemented.
