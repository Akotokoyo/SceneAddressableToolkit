using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnerManager : MonoBehaviour
{
    public ZoneConfig zoneConfig;

    void Start()
    {
        if(zoneConfig == null)
        {
            Debug.LogWarning("Zone Configuration not setted, skip loading zone from config");
            return;
        }

        LoadZoneConfig(zoneConfig);
    }

    public void LoadZoneConfig(ZoneConfig config)
    {
        zoneConfig = config;
        if (zoneConfig.SpawnEntries == null || zoneConfig.SpawnEntries.Count == 0)
        {
            return;
        }

        foreach (var entry in zoneConfig.SpawnEntries)
        {
            StartCoroutine(LoadAddressable(entry));
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
        }
    }

}
