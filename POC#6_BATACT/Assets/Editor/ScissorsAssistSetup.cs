#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ScissorsAssistSetup : Editor
{
    private const string PlayerPrefabPath = "Assets/Prefab/Player.prefab";

    [MenuItem("Tools/Setup Scissors Assist")]
    public static void SetupScissorsAssist()
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

            ScissorsTerrainGripAssist assist = playerRoot.GetComponent<ScissorsTerrainGripAssist>();
            if (assist == null)
            {
                assist = playerRoot.AddComponent<ScissorsTerrainGripAssist>();
            }

            WeaponTerrainProbe2D probe = playerRoot.GetComponentInChildren<WeaponTerrainProbe2D>();
            
            SerializedObject soAssist = new SerializedObject(assist);
            if (probe != null)
            {
                soAssist.FindProperty("terrainProbe").objectReferenceValue = probe;
            }
            soAssist.ApplyModifiedProperties();

            Debug.Log("<color=green><b>Scissors Assist Setup Completed Successfully!</b></color>");
        }
    }
}
#endif
