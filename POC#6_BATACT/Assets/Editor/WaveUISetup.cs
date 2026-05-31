using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WaveUISetup : MonoBehaviour
{
    [MenuItem("Tools/Legacy Setups/Setup Wave Status UI")]
    public static void SetupWaveUI()
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

        // 2. Check if Wave UI already exists
        WaveStatusUI existingUI = FindFirstObjectByType<WaveStatusUI>();
        if (existingUI != null)
        {
            Debug.LogWarning("WaveStatusUI already exists in the scene.");
            return;
        }

        // 3. Create Text Object
        GameObject textObj = new GameObject("WaveStatusUI");
        textObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        // Top Center
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f); 
        rect.sizeDelta = new Vector2(300f, 80f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Wave Loading...";
        text.alignment = TextAlignmentOptions.Top;
        text.fontSize = 24;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.richText = true;

        // Shadow for readability
        // Since TMP font material has Outline/Shadow, we just add a slight shadow offset in vertex if needed, 
        // but TMP is usually readable. We'll just leave it as standard white text.

        // 4. Add Logic Script
        textObj.AddComponent<WaveStatusUI>();

        // 5. Mark Scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("Successfully created Wave Status UI at the top center of the Canvas!");
    }
}
