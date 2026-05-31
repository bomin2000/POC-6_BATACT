using UnityEngine;
using UnityEditor;

public class RangedEnemySetup : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Setup Ranged Enemy Prefabs")]
    public static void SetupRangedEnemy()
    {
        // 1. Create EnemyProjectile.prefab
        string projectilePath = "Assets/Prefab/EnemyProjectile.prefab";
        GameObject projObj = new GameObject("EnemyProjectile");
        
        SpriteRenderer projSprite = projObj.AddComponent<SpriteRenderer>();
        // Just use a default sprite for now (e.g. Knob)
        projSprite.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        projSprite.color = new Color(1f, 0.3f, 0f, 1f); // Orange
        projObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        CircleCollider2D projCol = projObj.AddComponent<CircleCollider2D>();
        projCol.isTrigger = true;

        projObj.AddComponent<Rigidbody2D>();
        projObj.AddComponent<EnemyProjectile2D>();

        GameObject savedProj = PrefabUtility.SaveAsPrefabAsset(projObj, projectilePath);
        DestroyImmediate(projObj);

        // 2. Create EnemyRanged.prefab based on Enemy.prefab
        string baseEnemyPath = "Assets/Prefab/Enemy.prefab";
        string rangedEnemyPath = "Assets/Prefab/EnemyRanged.prefab";

        GameObject baseEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(baseEnemyPath);
        if (baseEnemy != null)
        {
            GameObject rangedObj = (GameObject)PrefabUtility.InstantiatePrefab(baseEnemy);
            PrefabUtility.UnpackPrefabInstance(rangedObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            rangedObj.name = "EnemyRanged";

            // Swap AI script
            EnemySideViewChaser oldAI = rangedObj.GetComponent<EnemySideViewChaser>();
            if (oldAI != null) DestroyImmediate(oldAI);

            EnemyRangedShooter newAI = rangedObj.AddComponent<EnemyRangedShooter>();

            // Find visual root to assign
            Transform visualRoot = rangedObj.transform.Find("Visual");
            if (visualRoot != null)
            {
                SerializedObject so = new SerializedObject(newAI);
                so.FindProperty("visualRoot").objectReferenceValue = visualRoot;
                
                // Change color slightly to distinguish
                SpriteRenderer[] srs = visualRoot.GetComponentsInChildren<SpriteRenderer>();
                foreach(var sr in srs) { sr.color = new Color(1f, 0.6f, 0.8f); } // Pinkish red
                so.ApplyModifiedProperties();
            }

            // Create FirePoint
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(rangedObj.transform);
            firePoint.transform.localPosition = new Vector3(0.5f, 0f, 0f); // slightly in front
            
            SerializedObject so2 = new SerializedObject(newAI);
            so2.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            so2.FindProperty("projectilePrefab").objectReferenceValue = savedProj;
            so2.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(rangedObj, rangedEnemyPath);
            DestroyImmediate(rangedObj);
            
            Debug.Log("Successfully created EnemyProjectile and EnemyRanged prefabs!");
        }
        else
        {
            Debug.LogError("Could not find base Enemy.prefab at " + baseEnemyPath);
        }
    }
}
