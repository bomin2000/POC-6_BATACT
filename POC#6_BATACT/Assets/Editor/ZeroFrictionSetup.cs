#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ZeroFrictionSetup : Editor
{
    [MenuItem("Tools/Legacy Setups/Fix Player Wall Sticking")]
    public static void ApplyZeroFriction()
    {
        string materialPath = "Assets/PhysicsMaterials/ZeroFriction2D.physicsMaterial2D";
        
        // 1. Ensure Folder Exists
        if (!AssetDatabase.IsValidFolder("Assets/PhysicsMaterials"))
        {
            AssetDatabase.CreateFolder("Assets", "PhysicsMaterials");
        }

        // 2. Create or Load Material
        PhysicsMaterial2D zeroFrictionMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
        if (zeroFrictionMat == null)
        {
            zeroFrictionMat = new PhysicsMaterial2D();
            zeroFrictionMat.friction = 0f;
            zeroFrictionMat.bounciness = 0f;
            AssetDatabase.CreateAsset(zeroFrictionMat, materialPath);
        }

        // 3. Apply to Player Prefab
        string prefabPath = "Assets/Prefab/Player.prefab";
        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject playerRoot = editingScope.prefabContentsRoot;
            CapsuleCollider2D col = playerRoot.GetComponent<CapsuleCollider2D>();
            
            if (col != null)
            {
                col.sharedMaterial = zeroFrictionMat;
                Debug.Log("<color=green><b>[Success]</b></color> Zero Friction Material applied to Player!");
            }
            else
            {
                Debug.LogError("[Error] CapsuleCollider2D not found on Player prefab.");
            }
        }
    }
}
#endif
