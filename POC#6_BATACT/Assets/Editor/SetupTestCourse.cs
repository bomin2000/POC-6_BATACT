using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupTestCourse : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Setup Test Course")]
    public static void CreateCourse()
    {
        // 1. Clean up old course
        GameObject oldCourse = GameObject.Find("TestCourse");
        if (oldCourse != null)
        {
            DestroyImmediate(oldCourse);
        }

        GameObject course = new GameObject("TestCourse");

        // Helper to create 2D blocks
        GameObject CreateBlock2D(string name, Vector3 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(course.transform);
            obj.transform.position = pos;
            
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;

            BoxCollider2D bc2d = obj.AddComponent<BoxCollider2D>();
            bc2d.size = size;
            return obj;
        }

        // 2. Ground
        GameObject ground = CreateBlock2D("Ground", new Vector3(25f, -2f, 0f), new Vector2(100f, 2f), new Color(0.2f, 0.2f, 0.2f));
        ground.layer = LayerMask.NameToLayer("Ground") > -1 ? LayerMask.NameToLayer("Ground") : 0;

        // 3. Left Wall
        GameObject leftWall = CreateBlock2D("LeftWall", new Vector3(-5f, 5f, 0f), new Vector2(2f, 20f), new Color(0.2f, 0.2f, 0.2f));
        leftWall.layer = ground.layer;

        // 4. Low Obstacle (requires jump/dash)
        GameObject obstacle = CreateBlock2D("LowObstacle", new Vector3(12f, -0.5f, 0f), new Vector2(2f, 1.5f), new Color(0.4f, 0.2f, 0.2f));
        obstacle.layer = ground.layer;

        // 5. Spawn Points
        GameObject spawnPointsRoot = new GameObject("SpawnPoints");
        spawnPointsRoot.transform.SetParent(course.transform);
        
        float[] spawnX = { 20f, 30f, 40f, 45f };
        Transform[] spawnTransforms = new Transform[spawnX.Length];
        for (int i = 0; i < spawnX.Length; i++)
        {
            GameObject sp = new GameObject($"SpawnPoint_{i+1}");
            sp.transform.SetParent(spawnPointsRoot.transform);
            sp.transform.position = new Vector3(spawnX[i], 3f, 0f); // High up to drop down
            spawnTransforms[i] = sp.transform;
        }

        // 6. Setup Spawner
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("spawnPoints").ClearArray();
            for (int i = 0; i < spawnTransforms.Length; i++)
            {
                so.FindProperty("spawnPoints").InsertArrayElementAtIndex(i);
                so.FindProperty("spawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];
            }
            so.ApplyModifiedProperties();
        }

        // 7. Move Player
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, 0f);
        }

        // 8. Move Goal
        StageGoalTrigger goal = FindFirstObjectByType<StageGoalTrigger>();
        if (goal == null)
        {
            Debug.LogWarning("No StageGoalTrigger found! Creating a temporary one at the end.");
            GameObject goalObj = new GameObject("StageGoal");
            goalObj.transform.position = new Vector3(55f, 2f, 0f);
            BoxCollider2D gCol = goalObj.AddComponent<BoxCollider2D>();
            gCol.isTrigger = true;
            gCol.size = new Vector2(2f, 10f);
            goalObj.AddComponent<StageGoalTrigger>();
            goal = goalObj.GetComponent<StageGoalTrigger>();
        }
        else
        {
            goal.transform.position = new Vector3(55f, 2f, 0f);
        }

        // 9. Camera Follow
        Camera cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                follow = cam.gameObject.AddComponent<CameraFollow2D>();
            }
            // Use reflection or standard assignment if possible
            var type = follow.GetType();
            var targetField = type.GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField != null && player != null)
            {
                targetField.SetValue(follow, player.transform);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Successfully created the Test Course! Press Play to try it out.");
    }
}
