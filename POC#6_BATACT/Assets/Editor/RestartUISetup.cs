using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RestartUISetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Game Restart UI")]
    public static void SetupRestartUI()
    {
        // 1. Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Find or create EventSystem
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        // 3. Check if Restart UI already exists
        GameRestartUI existingUI = FindFirstObjectByType<GameRestartUI>();
        if (existingUI != null)
        {
            Debug.LogWarning("GameRestartUI already exists in the scene.");
            return;
        }

        // 4. Create Button
        GameObject buttonObj = new GameObject("RestartButton");
        buttonObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f); // Top Right corner, slightly inset
        rect.sizeDelta = new Vector2(120f, 40f);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // Dark gray background

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        button.colors = colors;

        // 5. Create Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = "Restart (R)";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 16;

        // 6. Add Logic Script
        GameRestartUI restartLogic = buttonObj.AddComponent<GameRestartUI>();
        SerializedObject so = new SerializedObject(restartLogic);
        so.FindProperty("restartButton").objectReferenceValue = button;
        so.ApplyModifiedProperties();

        // 7. Mark Scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("Successfully created Restart Button at the top right of the Canvas!");
    }
}
