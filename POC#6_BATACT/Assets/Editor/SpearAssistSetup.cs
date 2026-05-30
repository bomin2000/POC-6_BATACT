#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SpearAssistSetup : Editor
{
    private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";

    [MenuItem("Tools/Setup Spear Assist")]
    public static void SetupSpearAssist()
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

            SpearTerrainMovementAssist assist = playerRoot.GetComponent<SpearTerrainMovementAssist>();
            if (assist == null)
            {
                assist = playerRoot.AddComponent<SpearTerrainMovementAssist>();
            }

            WeaponTerrainProbe2D probe = playerRoot.GetComponentInChildren<WeaponTerrainProbe2D>();
            
            SerializedObject soAssist = new SerializedObject(assist);
            if (probe != null)
            {
                soAssist.FindProperty("terrainProbe").objectReferenceValue = probe;
            }
            soAssist.ApplyModifiedProperties();

            Debug.Log("<color=green><b>Spear Assist Setup Completed Successfully!</b></color>");
        }
    }
}
#endif
