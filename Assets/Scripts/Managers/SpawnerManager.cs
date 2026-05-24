using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnerManager : MonoBehaviour
{
    public ZoneConfig zoneConfig;
    [SerializeField] private ZoneLODManager zoneLODManager;
    [SerializeField] private Transform playerTransform;

    [SerializeField] private int maxConcurrentAddressableLoads = 4; //0 min.

    private readonly List<GameObject> _spawnedInstances = new();

    void Start()
    {
        if (zoneConfig == null)
        {
            Debug.LogWarning("Zone Configuration not setted, skip loading zone from config");
            return;
        }

        if (zoneLODManager == null)
        {
            zoneLODManager = FindObjectOfType<ZoneLODManager>();
        }

        LoadZoneConfig(zoneConfig);
    }

    public void UnloadZone()
    {
        StopAllCoroutines();

        foreach (GameObject instance in _spawnedInstances)
        {
            if (instance != null)
            {
                Addressables.ReleaseInstance(instance);
            }
        }

        _spawnedInstances.Clear();
        zoneLODManager?.ClearTrackedObjects();
        zoneConfig = null;
    }

    public void LoadZoneConfig(ZoneConfig config)
    {
        zoneConfig = config;
        if (zoneConfig.SpawnEntries == null || zoneConfig.SpawnEntries.Count == 0)
        {
            return;
        }

        if(playerTransform == null)
        {
            Debug.LogWarning("[ZoneSpawner] No player found - use flat order with world origin(0,0,0)");
            LoadFlatOrdered(Vector3.zero);
            return;
        }

        if(zoneConfig.ChunkSizeWorld <= 0f)
        {
            LoadFlatOrdered(playerTransform.position);
        }
        else
        {
            StartCoroutine(LoadStagedByChunks());
        }
    }

    private void LoadFlatOrdered(Vector3 sortOrigin)
    {
        var ordered = zoneConfig.SpawnEntries.OrderBy(e => (e.Position - sortOrigin).sqrMagnitude).ToList();

        StartCoroutine(LoadEntriesWithConcurrencyLimit(ordered));
    }

    private IEnumerator LoadStagedByChunks()
    {
        Vector3 sortOrigin = playerTransform.position;
        Vector2Int playerChunk = ZoneChunkUtility.WorldToChunkXZ(
            sortOrigin,
            zoneConfig.ChunkGridOrigin,
            zoneConfig.ChunkSizeWorld);

        int ring = Mathf.Max(0, zoneConfig.InitialLoadChunkRing);

        List<SpawnEntry> critical = new();
        List<SpawnEntry> deferred = new();

        foreach (SpawnEntry entry in zoneConfig.SpawnEntries)
        {
            Vector2Int cell = ZoneChunkUtility.WorldToChunkXZ(
                entry.Position,
                zoneConfig.ChunkGridOrigin,
                zoneConfig.ChunkSizeWorld);

            int distance = ZoneChunkUtility.ChebyshevDistanceChunks(playerChunk, cell);
            if (distance <= ring)
            {
                critical.Add(entry);
            }
            else
            {
                deferred.Add(entry);
            }
        }

        critical.Sort(
            (a, b) => (a.Position - sortOrigin).sqrMagnitude.CompareTo((b.Position - sortOrigin).sqrMagnitude));
        deferred.Sort(
            (a, b) => (a.Position - sortOrigin).sqrMagnitude.CompareTo((b.Position - sortOrigin).sqrMagnitude));


        yield return LoadEntriesWithConcurrencyLimit(critical);
        yield return LoadEntriesWithConcurrencyLimit(deferred);
    }

    private IEnumerator LoadEntriesWithConcurrencyLimit(IReadOnlyList<SpawnEntry> entries)
    {
        if(entries == null || entries.Count == 0)
        {
            yield break;
        }

        if(maxConcurrentAddressableLoads <= 0)
        {
            int pending = entries.Count;
            foreach(var e in entries)
            {
                StartCoroutine(RunLoadThenRelease(e, () => pending--));
            }
            yield return new WaitUntil(() => pending <= 0);
            yield break;
        }

        Queue<SpawnEntry> queue = new(entries);
        int inFlight = 0;

        while(queue.Count > 0 || inFlight > 0)
        {
            while(inFlight < maxConcurrentAddressableLoads && queue.Count > 0)
            {
                SpawnEntry entry = queue.Dequeue();
                inFlight++;
                StartCoroutine(RunLoadThenRelease(entry, () => inFlight--));
            }

            yield return null;
        }
    }

    private IEnumerator RunLoadThenRelease(SpawnEntry entry, Action releaseSlot)
    {
        try
        {
            yield return LoadAddressable(entry);
        }
        finally
        {
            releaseSlot?.Invoke();
        }
    }


    private IEnumerator LoadAddressable(SpawnEntry entry)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(entry.PrefabReference);
        yield return handle;

        if(handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject instantiateObject = handle.Result;
            instantiateObject.transform.position = entry.Position;
            instantiateObject.transform.rotation = Quaternion.Euler(entry.Rotation);
            instantiateObject.transform.localScale = entry.Scaling;
            instantiateObject.name = entry.Name;

            _spawnedInstances.Add(instantiateObject);
            zoneLODManager?.RegisterSpawnedObject(instantiateObject, entry);
        }
    }

}
