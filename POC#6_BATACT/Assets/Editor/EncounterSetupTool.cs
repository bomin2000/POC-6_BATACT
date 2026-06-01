using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class EncounterSetupTool : MonoBehaviour
{
    [MenuItem("Tools/Setups/Setup First Encounter Trigger")]
    public static void SetupEncounter()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        // Disable existing Spawner's auto start
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            SerializedObject soSpawner = new SerializedObject(spawner);
            soSpawner.Update();
            soSpawner.FindProperty("spawnOnStart").boolValue = false;
            soSpawner.ApplyModifiedProperties();
        }

        // Check if an encounter already exists or create one
        EncounterTrigger2D trigger = FindFirstObjectByType<EncounterTrigger2D>();
        GameObject encounterObj;

        if (trigger != null)
        {
            encounterObj = trigger.gameObject;
            Debug.Log("Found existing EncounterTrigger2D. Updating it with the Formation Profile.");
        }
        else
        {
            encounterObj = new GameObject("Encounter_Tutorial");
            encounterObj.transform.position = new Vector3(0f, -2f, 0f);

            BoxCollider2D box = encounterObj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(5f, 5f);
            box.offset = new Vector2(0f, 2.5f);

            trigger = encounterObj.AddComponent<EncounterTrigger2D>();
            trigger.encounterName = "Tutorial Ambush";
            trigger.isOneTimeOnly = true;
        }

        // Clean up any old child spawn points if they exist from the old version
        for (int i = encounterObj.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(encounterObj.transform.GetChild(i).gameObject);
        }

        // Load Prefabs
        GameObject basicEnemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EnemySideView.prefab");
        GameObject fastEnemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/FastEnemy.prefab");
        GameObject rangedEnemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/RangedEnemy.prefab");

        if (basicEnemy == null) basicEnemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Enemy.prefab"); // Fallback

        // Create Profile Asset
        string profilePath = "Assets/ScriptableObjects/TutorialFormation.asset";
        EncounterFormationProfile profile = AssetDatabase.LoadAssetAtPath<EncounterFormationProfile>(profilePath);
        if (profile == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            
            profile = ScriptableObject.CreateInstance<EncounterFormationProfile>();
            profile.formationName = "Tutorial Basic Group";
            profile.spawns = new FormationSpawnData[3];
            
            profile.spawns[0] = new FormationSpawnData { enemyPrefab = basicEnemy, localOffset = new Vector2(8f, 0f), delay = 0f };
            profile.spawns[1] = new FormationSpawnData { enemyPrefab = fastEnemy != null ? fastEnemy : basicEnemy, localOffset = new Vector2(-8f, 0f), delay = 0.5f };
            profile.spawns[2] = new FormationSpawnData { enemyPrefab = rangedEnemy != null ? rangedEnemy : basicEnemy, localOffset = new Vector2(12f, 3f), delay = 1.0f };

            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();
        }

        trigger.formationProfile = profile;

        EditorUtility.SetDirty(encounterObj);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Successfully created 'Encounter_Tutorial' with a Formation Profile in SampleScene!");
    }
}
