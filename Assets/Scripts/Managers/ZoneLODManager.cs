using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class ZoneLODManager : MonoBehaviour
{
    [SerializeField] private Transform playertransform;
    [SerializeField] private float upldateInterval = 0.3f;
    [SerializeField] private float reactivateDistanceMultiplier = 0.9f;

    private readonly List<TrackedSpawnedObject> trackedObject = new();
    private float nextUpdateTime;

    private class TrackedSpawnedObject
    {
        public GameObject Instance;
        public SpawnEntry Entry;
        public bool IsCulled;

        public TrackedSpawnedObject(GameObject instance, SpawnEntry entry)
        {
            Instance = instance;
            Entry = entry;
            IsCulled = false;
        }
    }

    public void RegisterSpawnedObject(GameObject instance, SpawnEntry entry)
    {
        if(instance == null || entry == null)
        {
            return;
        }

        trackedObject.Add(new TrackedSpawnedObject(instance, entry));
    }

    public void UnRegisterSpawnedObject(GameObject instance)
    {
        if (instance == null) return;
        trackedObject.RemoveAll(x=> x.Instance == instance);
    }

    public void ClearTrackedObjects()
    {
        trackedObject.Clear();
    }

    void Awake()
    {
        if(playertransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if(player != null)
            {
                playertransform = player.transform;
            }
        }
    }

    void Update()
    {
        if (playertransform == null) return;
        if (Time.time < nextUpdateTime) return;

        nextUpdateTime = Time.time + upldateInterval;
        UpdateCulling();
    }

    private void UpdateCulling()
    {
        for(int i = 0; i< trackedObject.Count -1; i++)
        {
            TrackedSpawnedObject tracked = trackedObject[i];

            if(tracked.Instance == null)
            {
                trackedObject.RemoveAt(i);
                continue;
            }

            if(!tracked.Entry.HasCullingDistance || tracked.Entry.CullingDistance <= 0f)
            {
                continue;
            }

            float sqrDistance = (tracked.Instance.transform.position - playertransform.transform.position).sqrMagnitude;
            float cullingDistance = tracked.Entry.CullingDistance;
            float cullingDistanceSqr = cullingDistance * cullingDistance;
            float reactivateDistance = cullingDistance * reactivateDistanceMultiplier;
            float reactiveDistanceSqr = reactivateDistance * reactivateDistance;

            if(!tracked.IsCulled && sqrDistance > cullingDistance)
            {
                tracked.Instance.SetActive(false);
                tracked.IsCulled = true;
            }
            else if(tracked.IsCulled && sqrDistance < cullingDistance)
            {
                tracked.Instance.SetActive(true);
                tracked.IsCulled = false;
            }
        }
    }
}


