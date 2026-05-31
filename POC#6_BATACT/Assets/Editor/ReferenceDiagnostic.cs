#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ReferenceDiagnostic : Editor
{
    [MenuItem("Tools/Legacy Setups/Run Diagnostics/Verify Spear Assist Links")]
    public static void VerifyLinks()
    {
        Debug.Log("<b>[Diagnostic] Starting Spear Assist Reference Check...</b>");

        // 1. Check Player Prefab
        string prefabPath = "Assets/Prefab/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError($"[Diagnostic] FAILED: Could not find Player prefab at {prefabPath}");
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = editingScope.prefabContentsRoot;
            bool needsSave = false;

            // Check SpearTerrainMovementAssist
            SpearTerrainMovementAssist assist = root.GetComponent<SpearTerrainMovementAssist>();
            if (assist == null)
            {
                Debug.LogError("[Diagnostic] FAILED: SpearTerrainMovementAssist is MISSING from Player root!");
            }
            else
            {
                Debug.Log("<color=green>[Diagnostic] OK:</color> SpearTerrainMovementAssist is attached to Player root.");
            }

            // Check Rigidbody2D
            Rigidbody2D rb = root.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("[Diagnostic] FAILED: Rigidbody2D is MISSING from Player root!");
            }
            else
            {
                Debug.Log("<color=green>[Diagnostic] OK:</color> Rigidbody2D is present on Player root.");
            }

            // Check WeaponTerrainProbe2D
            WeaponTerrainProbe2D probe = root.GetComponentInChildren<WeaponTerrainProbe2D>(true);
            if (probe == null)
            {
                Debug.LogError("[Diagnostic] FAILED: WeaponTerrainProbe2D is MISSING from Player children!");
            }
            else
            {
                Debug.Log($"<color=green>[Diagnostic] OK:</color> WeaponTerrainProbe2D found on child: {probe.gameObject.name}");
                
                SerializedObject soProbe = new SerializedObject(probe);
                Object controllerRef = soProbe.FindProperty("weaponController").objectReferenceValue;
                if (controllerRef == null)
                {
                    Debug.LogWarning("[Diagnostic] WARNING: WeaponTerrainProbe2D is missing reference to DualBladeWeaponController. Auto-fixing...");
                    soProbe.FindProperty("weaponController").objectReferenceValue = root.GetComponent<DualBladeWeaponController>();
                    soProbe.ApplyModifiedProperties();
                    needsSave = true;
                }
                else
                {
                    Debug.Log("<color=green>[Diagnostic] OK:</color> Probe weaponController reference is intact.");
                }

                int terrainMask = soProbe.FindProperty("terrainLayers").intValue;
                int groundMask = 1 << LayerMask.NameToLayer("Ground");
                if (terrainMask != groundMask)
                {
                    Debug.LogWarning($"[Diagnostic] WARNING: Probe terrainLayers is not explicitly 'Ground' only (Value: {terrainMask}). Auto-fixing...");
                    soProbe.FindProperty("terrainLayers").intValue = groundMask;
                    soProbe.ApplyModifiedProperties();
                    needsSave = true;
                }
                else
                {
                    Debug.Log("<color=green>[Diagnostic] OK:</color> Probe terrainLayers is correctly set to Ground only.");
                }
            }

            // Check Assist Probe Link
            if (assist != null && probe != null)
            {
                SerializedObject soAssist = new SerializedObject(assist);
                Object probeRef = soAssist.FindProperty("terrainProbe").objectReferenceValue;
                if (probeRef == null || probeRef != probe)
                {
                    Debug.LogWarning("[Diagnostic] WARNING: SpearTerrainMovementAssist is missing link to WeaponTerrainProbe2D. Auto-fixing...");
                    soAssist.FindProperty("terrainProbe").objectReferenceValue = probe;
                    soAssist.ApplyModifiedProperties();
                    needsSave = true;
                }
                else
                {
                    Debug.Log("<color=green>[Diagnostic] OK:</color> SpearTerrainMovementAssist has valid link to WeaponTerrainProbe2D.");
                }
            }
        }

        // 2. Check Scene Test Environment (if currently open)
        GameObject floor = GameObject.Find("Test_GroundFloor");
        if (floor != null)
        {
            if (floor.layer != LayerMask.NameToLayer("Ground"))
            {
                Debug.LogWarning("[Diagnostic] WARNING: Test_GroundFloor layer is NOT 'Ground'. Auto-fixing...");
                floor.layer = LayerMask.NameToLayer("Ground");
            }
            Collider2D col = floor.GetComponent<Collider2D>();
            if (col == null)
                Debug.LogError("[Diagnostic] FAILED: Test_GroundFloor is missing a Collider2D!");
            else if (col.isTrigger)
                Debug.LogWarning("[Diagnostic] WARNING: Test_GroundFloor collider is set to Trigger. Should be solid!");
        }

        GameObject wall = GameObject.Find("Test_GroundWall");
        if (wall != null)
        {
            if (wall.layer != LayerMask.NameToLayer("Ground"))
            {
                Debug.LogWarning("[Diagnostic] WARNING: Test_GroundWall layer is NOT 'Ground'. Auto-fixing...");
                wall.layer = LayerMask.NameToLayer("Ground");
            }
            Collider2D col = wall.GetComponent<Collider2D>();
            if (col == null)
                Debug.LogError("[Diagnostic] FAILED: Test_GroundWall is missing a Collider2D!");
            else if (col.isTrigger)
                Debug.LogWarning("[Diagnostic] WARNING: Test_GroundWall collider is set to Trigger. Should be solid!");
        }

        Debug.Log("<b>[Diagnostic] Check Complete!</b> If any warnings were auto-fixed, please re-test.");
    }
}
#endif
