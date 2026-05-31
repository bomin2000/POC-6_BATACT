#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class WeaponLimiterSetup : Editor
{
    private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";

    [MenuItem("Tools/Legacy Setups/Setup Weapon Contact Limiter")]
    public static void SetupWeaponLimiter()
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"Could not find Player prefab at {PlayerPrefabPath}");
            return;
        }

        using (var editingScope = new PrefabUtility.EditPrefabContentsScope(PlayerPrefabPath))
        {
            GameObject playerRoot = editingScope.prefabContentsRoot;

            WeaponTerrainContactLimiter2D limiter = playerRoot.GetComponent<WeaponTerrainContactLimiter2D>();
            if (limiter == null)
            {
                limiter = playerRoot.AddComponent<WeaponTerrainContactLimiter2D>();
            }

            WeaponVerticalEscapeAssist escapeAssist = playerRoot.GetComponent<WeaponVerticalEscapeAssist>();
            if (escapeAssist == null)
            {
                escapeAssist = playerRoot.AddComponent<WeaponVerticalEscapeAssist>();
            }

            WeaponEscapeSpaceChecker2D spaceChecker = playerRoot.GetComponent<WeaponEscapeSpaceChecker2D>();
            if (spaceChecker == null)
            {
                spaceChecker = playerRoot.AddComponent<WeaponEscapeSpaceChecker2D>();
            }

            PlayerWallSlideAssist wallSlideAssist = playerRoot.GetComponent<PlayerWallSlideAssist>();
            if (wallSlideAssist == null)
            {
                wallSlideAssist = playerRoot.AddComponent<PlayerWallSlideAssist>();
            }

            DualBladeWeaponController controller = playerRoot.GetComponent<DualBladeWeaponController>();
            if (controller != null)
            {
                SerializedObject soController = new SerializedObject(controller);
                soController.FindProperty("contactLimiter").objectReferenceValue = limiter;
                soController.ApplyModifiedProperties();
            }

            Debug.Log("<color=green><b>Weapon Terrain Contact Limiter Setup Completed Successfully!</b></color>");
        }
    }
}
#endif
