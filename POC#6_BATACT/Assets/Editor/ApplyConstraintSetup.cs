using UnityEngine;
using UnityEditor;

public class ApplyConstraintSetup : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Apply Weapon Constraint Setup")]
    public static void RunSetup()
    {
        string prefabPath = "Assets/Prefab/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab != null)
        {
            // Remove old scripts
            DestroyImmediate(playerPrefab.GetComponent<SpearTerrainMovementAssist>(), true);
            DestroyImmediate(playerPrefab.GetComponent<ScissorsTerrainGripAssist>(), true);
            DestroyImmediate(playerPrefab.GetComponent<WeaponVerticalEscapeAssist>(), true);
            DestroyImmediate(playerPrefab.GetComponent<WeaponTerrainContactLimiter2D>(), true);
            DestroyImmediate(playerPrefab.GetComponentInChildren<WeaponTerrainProbe2D>(), true);

            // Add new constraint script
            WeaponTerrainConstraint2D constraint = playerPrefab.GetComponent<WeaponTerrainConstraint2D>();
            if (constraint == null)
            {
                constraint = playerPrefab.AddComponent<WeaponTerrainConstraint2D>();
                constraint.terrainLayers = LayerMask.GetMask("Default", "Ground", "Platform"); // Common terrain layers
            }

            // Hook it up to controller
            DualBladeWeaponController controller = playerPrefab.GetComponent<DualBladeWeaponController>();
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                so.Update();
                so.FindProperty("terrainConstraint").objectReferenceValue = constraint;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(playerPrefab);
            PrefabUtility.SavePrefabAsset(playerPrefab);
            Debug.Log("Successfully replaced legacy assist scripts with WeaponTerrainConstraint2D!");
        }
    }
}
