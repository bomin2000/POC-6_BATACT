using UnityEngine;
using UnityEditor;

public class ProjectileSetup : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Setup Projectile Visuals")]
    public static void SetupProjectile()
    {
        string path = "Assets/Prefab/EnemyProjectile.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        
        if (prefab == null)
        {
            Debug.LogWarning($"Could not find {path}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // 1. Check if Visual already exists
        Transform visualRoot = instance.transform.Find("Visual");
        SpriteRenderer rootRenderer = instance.GetComponent<SpriteRenderer>();

        if (visualRoot == null)
        {
            GameObject visualObj = new GameObject("Visual");
            visualRoot = visualObj.transform;
            visualRoot.SetParent(instance.transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;

            SpriteRenderer visualRenderer = visualObj.AddComponent<SpriteRenderer>();
            
            // Move sprite info from root to visual if root has it
            if (rootRenderer != null)
            {
                visualRenderer.sprite = rootRenderer.sprite;
                visualRenderer.color = rootRenderer.color;
                visualRenderer.sortingOrder = rootRenderer.sortingOrder;
                visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
                visualRenderer.flipX = rootRenderer.flipX;
                visualRenderer.flipY = rootRenderer.flipY;
                
                // Remove root renderer
                DestroyImmediate(rootRenderer);
            }
        }
        else
        {
            // If visual already exists, make sure root doesn't have a renderer
            if (rootRenderer != null)
            {
                DestroyImmediate(rootRenderer);
            }
        }

        // 2. Assign visualRoot to EnemyProjectile2D
        EnemyProjectile2D projectileScript = instance.GetComponent<EnemyProjectile2D>();
        if (projectileScript != null)
        {
            SerializedObject soProj = new SerializedObject(projectileScript);
            soProj.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            soProj.ApplyModifiedProperties();
        }

        // 3. Save Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, path);
        DestroyImmediate(instance);

        Debug.Log("Successfully separated Visuals for EnemyProjectile prefab!");
    }
}
