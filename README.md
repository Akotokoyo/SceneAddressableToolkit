# SceneAddressableToolkit

Unity toolkit to describe a game zone with a `ScriptableObject` and Addressables references.

## Current status

At the moment, the project mainly exposes one configuration asset: `ZoneConfig`.

This asset is used to collect zone metadata and content (map, spawns, size tier) in a single place, useful for open-world and level-design pipelines.

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

## Recommended workflow (MVP)

1. Create a `ZoneConfig` asset (Create > ScriptableObject, if a dedicated menu exists in the project).
2. Set `ZoneName`, `ZoneId`, and `MapReference`.
3. Fill `SpawnEntries` with Addressables prefabs and spawn data.
4. Set or compute `sizeTier` based on the number of elements.
5. Use the asset as a data source in zone streaming/loading systems.

## Note

- The toolkit and documentation are still in an early stage.
