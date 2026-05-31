using UnityEngine;
using UnityEditor;

public class FastEnemySetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Fast Enemy Prefab")]
    public static void SetupFastEnemy()
    {
        string baseEnemyPath = "Assets/Prefab/Enemy.prefab";
        string fastEnemyPath = "Assets/Prefab/EnemyFast.prefab";

        GameObject baseEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(baseEnemyPath);
        if (baseEnemy != null)
        {
            GameObject fastObj = (GameObject)PrefabUtility.InstantiatePrefab(baseEnemy);
            PrefabUtility.UnpackPrefabInstance(fastObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            fastObj.name = "EnemyFast";

            // 1. Modify AI Speed and Behavior
            EnemySideViewChaser chaser = fastObj.GetComponent<EnemySideViewChaser>();
            if (chaser != null)
            {
                SerializedObject so = new SerializedObject(chaser);
                so.FindProperty("moveSpeed").floatValue = 6f; // Faster (default 3.2)
                so.FindProperty("acceleration").floatValue = 50f; // Faster acceleration
                so.FindProperty("hopWhenBlocked").boolValue = false; // Do not jump
                so.ApplyModifiedProperties();
            }

            // 2. Modify Health
            PrototypeEnemyHitReceiver receiver = fastObj.GetComponent<PrototypeEnemyHitReceiver>();
            if (receiver != null)
            {
                SerializedObject so = new SerializedObject(receiver);
                so.FindProperty("maxHealth").floatValue = 50f; // Lower health (default 100)
                so.ApplyModifiedProperties();
            }

            // 3. Modify Visuals
            Transform visualRoot = fastObj.transform.Find("Visual");
            if (visualRoot != null)
            {
                // Make it slightly smaller
                visualRoot.localScale = new Vector3(0.85f, 0.85f, 1f);

                // Change color to Cyan to distinguish
                SpriteRenderer[] srs = visualRoot.GetComponentsInChildren<SpriteRenderer>();
                foreach(var sr in srs) { sr.color = new Color(0.2f, 1f, 1f); } 
            }

            // Save and clean up
            PrefabUtility.SaveAsPrefabAsset(fastObj, fastEnemyPath);
            DestroyImmediate(fastObj);
            
            Debug.Log("Successfully created EnemyFast prefab!");
        }
        else
        {
            Debug.LogError("Could not find base Enemy.prefab at " + baseEnemyPath);
        }
    }
}
