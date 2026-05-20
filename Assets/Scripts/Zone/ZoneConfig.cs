using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ZoneConfig : ScriptableObject
{
    public string ZoneName;
    public int ZoneId;
    public AssetReference MapReference;
    public List<SpawnEntry> SpawnEntries = new();


    [Tooltip("Zone Dimension Scale")]
    public ZoneSizeTier sizeTier = ZoneSizeTier.NONE;

    //Cell size in horizontal plane, if <=0, spawner will ignore the partitionament of chunk
    public float ChunkSizeWorld = 128f;

    //Grid Origin
    public Vector3 ChunkGridOrigin = Vector3.zero;

    //Chebyshev max chunk from player chunk, inside the first ring, will load the first wave, es. 2 = square 5x5 chunks
    public int InitialLoadChunkRing = 2;
}

[System.Serializable]
public class SpawnEntry
{
    public string Name;
    public AssetReference PrefabReference;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scaling;
    public string Tag;
    public bool HasLodOverride;
    public int LodOverride;
    public bool HasCullingDistance;
    public float CullingDistance;
    public int LayerIndex;
}

public enum ZoneSizeTier
{
    NONE = 0,   //0 Entries
    SMALL = 1,  //1k Entries
    MEDIUM = 2, //10k Entries
    BIG = 3     //More than 10k Entries
}

public static class ZonSizeTierClassification
{
    public const int SmallMaxEntriesInclusive = 1000;
    public const int MediumMaxEntriesInclusive = 10000;

    public static ZoneSizeTier GetSizeFromEntries(int entryCount)
    {
        if (entryCount < 0)
            return ZoneSizeTier.NONE;
        if (entryCount < SmallMaxEntriesInclusive)
            return ZoneSizeTier.SMALL;
        if (entryCount < MediumMaxEntriesInclusive)
            return ZoneSizeTier.MEDIUM;
        return ZoneSizeTier.BIG;
    }
}