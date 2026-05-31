using UnityEngine;
using UnityEditor;

public class SetupStageGoal : MonoBehaviour
{
    [MenuItem("Tools/Setup Stage Goal")]
    public static void SetupGoal()
    {
        StageGoalTrigger existing = FindFirstObjectByType<StageGoalTrigger>();
        if (existing != null)
        {
            Debug.LogWarning("A Stage Goal already exists in the scene.");
            EditorGUIUtility.PingObject(existing.gameObject);
            return;
        }

        GameObject goalObj = new GameObject("StageGoal");
        goalObj.transform.position = new Vector3(15f, 0f, 0f); // Default to the right

        BoxCollider2D col = goalObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 10f); // Tall trigger to easily catch the player

        SpriteRenderer renderer = goalObj.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        renderer.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(2f, 10f);
        renderer.sortingOrder = -10;

        goalObj.AddComponent<StageGoalTrigger>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("Successfully created Stage Goal! Move it to your desired end point.");
        Selection.activeGameObject = goalObj;
        EditorGUIUtility.PingObject(goalObj);
    }
}
