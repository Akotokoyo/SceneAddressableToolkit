using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneEditorGeneration : EditorWindow
{
    private const string MenuPath = "ToolKit/Generate Config Zone Form and To Scene";

    private static bool includeChildren = false;
    private static string zoneName = string.Empty;
    private static int zoneId = -1;

    private static ZoneConfig zoneConfig = null;

    [MenuItem(MenuPath)]
    static void Init()
    {
        GetWindow<ZoneEditorGeneration>();
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Zone From Scene", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        includeChildren = EditorGUILayout.Toggle("Include Children: ", includeChildren);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        zoneName = EditorGUILayout.TextField("Zone Name: ", zoneName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        zoneId = EditorGUILayout.IntField("Zone Id: ", zoneId);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if(GUILayout.Button("Generate Zone from Scene"))
        {
            GenerateZoneFromScene();
        }

        GUILayout.Space(10);

        GUILayout.Label("Generate Scene From Zone", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        zoneConfig = (ZoneConfig)EditorGUILayout.ObjectField("Zone Config: ", zoneConfig, typeof(ZoneConfig), false);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Scene from Zone"))
        {
            GenerateSceneFromZone();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Recaulculate Size Tier"))
        {
            RecalculateSizeTier();
        }
    }

    private static void GenerateZoneFromScene() {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("No Active Loaded Scene.");
            return;
        }

        var savePath = EditorUtility.SaveFilePanelInProject(
            "Save Zone Config",
            scene.name + "_ZoneConfig",
            "asset",
            "Choose where to save ZoneConfig");

        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        ZoneConfig config = ScriptableObject.CreateInstance<ZoneConfig>();
        config.name = zoneName;
        config.ZoneId = zoneId;
        config.SpawnEntries = new List<SpawnEntry>();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) {
            Debug.LogWarning("AddressableAssetSettings not found");
            return;
        }

        GameObject world = GameObject.Find("World");
        if(world == null)
        {
            foreach(var root in scene.GetRootGameObjects())
            {
                CollectEntries(root.transform, config.SpawnEntries, settings, includeChildren);
            }
        }
        else
        {
            Transform worldTransform = world.transform;
            for (int i = 0 ; i< worldTransform.childCount; i++)
            {
                CollectEntries(worldTransform.GetChild(i).transform, config.SpawnEntries, settings, includeChildren);
            }
        }


        //Create the scriptableAsset
        AssetDatabase.CreateAsset(config, savePath);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Zone Config Generated in : {savePath} | Entries: {config.SpawnEntries.Count}");
    }

    private static void CollectEntries(Transform t, List<SpawnEntry> entries, AddressableAssetSettings settings, bool recurseForChildren)
    {
        GameObject go = t.gameObject;


        if (!go.CompareTag("EditorOnly"))
        {
            var entry = BuildSpawnEntry(go, settings);
            if(entry != null)
            {
                entries.Add(entry);
            }
        }

        if (!recurseForChildren) {
            return; 
        }

        for(int i = 0; i< t.childCount; i++)
        {
            CollectEntries(t.GetChild(i), entries, settings, true);
        }
    }

    private static SpawnEntry BuildSpawnEntry(GameObject sceneObject, AddressableAssetSettings settings)
    {
        var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(sceneObject) as GameObject;
        if(prefabSource == null)
        {
            Debug.LogWarning("No Prefab instance -> Skip");
            return null;
        }

        var assetPath = AssetDatabase.GetAssetPath(prefabSource);
        if(assetPath == null)
        {
            return null;
        }

        var guid = AssetDatabase.AssetPathToGUID(assetPath);
        var aaEntry = settings.FindAssetEntry(guid);

        if(aaEntry == null)
        {
            Debug.LogWarning($"Skip '{sceneObject.name}': prefab is not Addressable ({assetPath})");
        }

        bool hasLodOverride = false;
        int lodOverride = 0;
        bool hasCullingDistance = false;
        float cullingDistance = 0f;

        switch (sceneObject.tag)
        {
            case "Building":
                hasCullingDistance = true;
                cullingDistance = 180f;
                break;
            default:
                hasLodOverride = false;
                lodOverride = 0;
                hasCullingDistance = false;
                cullingDistance = 0;
                break;
        }

        return new SpawnEntry
        {
            Name = sceneObject.name,
            PrefabReference = new UnityEngine.AddressableAssets.AssetReference(guid),
            Position = sceneObject.transform.position,
            Rotation = sceneObject.transform.eulerAngles,
            Scaling = sceneObject.transform.lossyScale,
            Tag = sceneObject.tag,
            HasCullingDistance = hasCullingDistance,
            CullingDistance = cullingDistance,
            HasLodOverride = hasLodOverride,
            LodOverride = lodOverride
        };

    }

    private static void GenerateSceneFromZone() { }
    private static void RecalculateSizeTier() { }


}
