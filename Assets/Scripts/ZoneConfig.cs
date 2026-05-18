using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ZoneConfig : ScriptableObject
{
    public string ZoneName;
    public int ZoneId;
    public AssetReference MapReference;
    [SerializeField] List<SpawnEntry> SpawnEntries = new();


    [Tooltip("Zone Dimension Scale")]
    public ZoneSizeTier sizeTier = ZoneSizeTier.NONE;

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
        return ZoneSizeTier.NONE;
    }
}