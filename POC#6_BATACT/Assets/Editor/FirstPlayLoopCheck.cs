using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class FirstPlayLoopCheck : MonoBehaviour
{
    [MenuItem("Tools/Verify/1차 플레이 루프 검수 및 자동 연결")]
    public static void RunVerification()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        if (EditorSceneManager.GetActiveScene().path != scenePath)
        {
            EditorSceneManager.OpenScene(scenePath);
        }

        bool dirty = false;

        // 1. Stage Goal Trigger check
        StageGoalTrigger goal = FindFirstObjectByType<StageGoalTrigger>();
        if (goal != null)
        {
            Collider2D col = goal.GetComponent<Collider2D>();
            if (col == null)
            {
                col = goal.gameObject.AddComponent<BoxCollider2D>();
                dirty = true;
            }
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                dirty = true;
                Debug.Log("Set StageGoal Collider2D isTrigger to true.");
            }
        }
        else
        {
            Debug.LogError("No StageGoalTrigger found in the scene.");
        }

        // 2. GameResultUI explicit references
        GameResultUI resultUI = FindFirstObjectByType<GameResultUI>();
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        
        if (resultUI != null)
        {
            SerializedObject so = new SerializedObject(resultUI);
            so.Update();

            var textProp = so.FindProperty("resultText");
            var bgProp = so.FindProperty("bgImage");
            var playerProp = so.FindProperty("player");
            var goalProp = so.FindProperty("goal");

            if (textProp.objectReferenceValue == null)
            {
                TextMeshProUGUI text = resultUI.GetComponentInChildren<TextMeshProUGUI>(true);
                textProp.objectReferenceValue = text;
                dirty = true;
                Debug.Log("Linked resultText.");
            }

            if (bgProp.objectReferenceValue == null)
            {
                Image bg = resultUI.GetComponent<Image>();
                bgProp.objectReferenceValue = bg;
                dirty = true;
                Debug.Log("Linked bgImage.");
            }

            if (playerProp.objectReferenceValue == null && player != null)
            {
                playerProp.objectReferenceValue = player;
                dirty = true;
                Debug.Log("Linked player.");
            }

            if (goalProp.objectReferenceValue == null && goal != null)
            {
                goalProp.objectReferenceValue = goal;
                dirty = true;
                Debug.Log("Linked goal.");
            }

            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogError("No GameResultUI found in the scene.");
        }

        // 3. Enemy Spawner Wave Setup
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            SerializedObject so = new SerializedObject(spawner);
            so.Update();
            SerializedProperty wavesProp = so.FindProperty("waves");
            
            if (wavesProp.arraySize < 2)
            {
                wavesProp.arraySize = 2;
                dirty = true;
            }

            // Wave 0: Sequential
            SerializedProperty w0 = wavesProp.GetArrayElementAtIndex(0);
            w0.FindPropertyRelative("waveName").stringValue = "Wave 1 - Sequential";
            w0.FindPropertyRelative("spawnMode").enumValueIndex = (int)WaveSpawnMode.Sequential;
            if (w0.FindPropertyRelative("basicEnemyCount").intValue == 0) w0.FindPropertyRelative("basicEnemyCount").intValue = 3;
            if (w0.FindPropertyRelative("spawnInterval").floatValue < 0.1f) w0.FindPropertyRelative("spawnInterval").floatValue = 1.5f;

            // Wave 1: AllAtOnce
            SerializedProperty w1 = wavesProp.GetArrayElementAtIndex(1);
            w1.FindPropertyRelative("waveName").stringValue = "Wave 2 - AllAtOnce";
            w1.FindPropertyRelative("spawnMode").enumValueIndex = (int)WaveSpawnMode.AllAtOnce;
            if (w1.FindPropertyRelative("fastEnemyCount").intValue == 0) w1.FindPropertyRelative("fastEnemyCount").intValue = 2;

            so.ApplyModifiedProperties();
            dirty = true;
            Debug.Log("Verified EnemySpawner waves (1 Sequential, 1 AllAtOnce).");
        }
        else
        {
            Debug.LogWarning("No EnemySpawner found in the scene.");
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Scene saved with stabilized connections.");
        }
        else
        {
            Debug.Log("Everything is already connected and stable.");
        }
    }
}
