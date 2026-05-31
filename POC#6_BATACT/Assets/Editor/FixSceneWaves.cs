using UnityEngine;
using UnityEditor;

public class FixSceneWaves : MonoBehaviour
{
    [MenuItem("Tools/Fix Scene Wave Settings")]
    public static void FixWaves()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("No EnemySpawner found in the scene.");
            return;
        }

        SerializedObject so = new SerializedObject(spawner);
        SerializedProperty wavesProp = so.FindProperty("waves");
        
        // Ensure array has 3 waves
        wavesProp.arraySize = 3;

        // Wave 1: Sequential
        SerializedProperty w1 = wavesProp.GetArrayElementAtIndex(0);
        w1.FindPropertyRelative("waveName").stringValue = "Wave 1 - Basics";
        w1.FindPropertyRelative("spawnMode").enumValueIndex = (int)WaveSpawnMode.Sequential;
        w1.FindPropertyRelative("basicEnemyCount").intValue = 3;
        w1.FindPropertyRelative("fastEnemyCount").intValue = 0;
        w1.FindPropertyRelative("rangedEnemyCount").intValue = 0;
        w1.FindPropertyRelative("spawnInterval").floatValue = 1f;
        w1.FindPropertyRelative("waveDelay").floatValue = 1f;
        w1.FindPropertyRelative("nextWaveDelay").floatValue = 2f;

        // Wave 2: AllAtOnce
        SerializedProperty w2 = wavesProp.GetArrayElementAtIndex(1);
        w2.FindPropertyRelative("waveName").stringValue = "Wave 2 - Ambush";
        w2.FindPropertyRelative("spawnMode").enumValueIndex = (int)WaveSpawnMode.AllAtOnce;
        w2.FindPropertyRelative("basicEnemyCount").intValue = 1;
        w2.FindPropertyRelative("fastEnemyCount").intValue = 2;
        w2.FindPropertyRelative("rangedEnemyCount").intValue = 0;
        w2.FindPropertyRelative("spawnInterval").floatValue = 1f;
        w2.FindPropertyRelative("waveDelay").floatValue = 1f;
        w2.FindPropertyRelative("nextWaveDelay").floatValue = 2f;

        // Wave 3: Sequential
        SerializedProperty w3 = wavesProp.GetArrayElementAtIndex(2);
        w3.FindPropertyRelative("waveName").stringValue = "Wave 3 - Combined";
        w3.FindPropertyRelative("spawnMode").enumValueIndex = (int)WaveSpawnMode.Sequential;
        w3.FindPropertyRelative("basicEnemyCount").intValue = 2;
        w3.FindPropertyRelative("fastEnemyCount").intValue = 1;
        w3.FindPropertyRelative("rangedEnemyCount").intValue = 2;
        w3.FindPropertyRelative("spawnInterval").floatValue = 1f;
        w3.FindPropertyRelative("waveDelay").floatValue = 1f;
        w3.FindPropertyRelative("nextWaveDelay").floatValue = 2f;

        so.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("Successfully updated EnemySpawner wave settings in the scene!");
    }
}
