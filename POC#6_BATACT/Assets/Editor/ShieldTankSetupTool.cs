using UnityEditor;
using UnityEngine;
using System.IO;

public class ShieldTankSetupTool
{
    [MenuItem("Tools/BAT/Create ShieldTank Prefab (Fix 3)")]
    public static void CreateShieldTank()
    {
        string destPath = "Assets/Prefab/EnemyShieldTank.prefab";

        GameObject instance = AssetDatabase.LoadAssetAtPath<GameObject>(destPath);
        if (instance == null) {
            Debug.LogError("ShieldTank prefab not found! Did you delete it?");
            return;
        }
        
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(instance);
        
        // Find Visual Root
        Transform visualRoot = prefabInstance.transform.Find("visualRoot") ?? prefabInstance.transform.Find("body");
        if (visualRoot == null) visualRoot = prefabInstance.transform;

        // Find or create Shield
        Transform shieldObj = prefabInstance.transform.Find("Shield");
        if (shieldObj == null) shieldObj = visualRoot.Find("Shield");
        
        if (shieldObj != null) {
            // Keep user's custom collider/visual, just ensure it's parented to visualRoot!
            if (shieldObj.parent != visualRoot) {
                shieldObj.SetParent(visualRoot, false);
            }
            
            // Add Health Bar if missing
            var hb = shieldObj.GetComponent<PartHealthBar2D>();
            if (hb == null) {
                hb = shieldObj.gameObject.AddComponent<PartHealthBar2D>();
                
                // Expose properties to make it visible
                var serializedHb = new SerializedObject(hb);
                serializedHb.FindProperty("hideWhenFull").boolValue = false; // Always show for debugging/clarity
                serializedHb.FindProperty("worldOffset").vector3Value = new Vector3(0f, 0.7f, 0f);
                serializedHb.ApplyModifiedProperties();
            }
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, destPath);
        Object.DestroyImmediate(prefabInstance);
        
        Debug.Log("ShieldTank fixed: Shield health bar added and re-parented to visualRoot!");
    }
}
