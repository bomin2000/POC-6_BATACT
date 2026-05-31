using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ApplyFinalSetup
{
    [MenuItem("Tools/Legacy Setups/Apply Final Game Result Setup")]
    public static void RunSetup()
    {
        // 1. Setup Stage Goal
        SetupStageGoal.SetupGoal();

        // 2. Setup Game Result UI
        ResultUISetup.SetupResultUI();

        // 3. Modify Player Prefab
        string prefabPath = "Assets/Prefab/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (playerPrefab != null)
        {
            PlayerHealth health = playerPrefab.GetComponent<PlayerHealth>();
            if (health != null)
            {
                SerializedObject so = new SerializedObject(health);
                so.Update();
                so.FindProperty("respawnOnDeath").boolValue = false;
                so.FindProperty("enableFallDeath").boolValue = true;
                so.FindProperty("fallDeathHeight").floatValue = -10f;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerPrefab);
                PrefabUtility.SavePrefabAsset(playerPrefab);
                Debug.Log("Updated Player.prefab: respawnOnDeath = false, enableFallDeath = true");
            }
        }
        else
        {
            Debug.LogError("Could not find Player.prefab at " + prefabPath);
        }

        // Save the scene
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Final Setup Complete and Scene Saved.");
    }
}
