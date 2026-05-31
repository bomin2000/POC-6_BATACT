using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class ResultUISetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Game Result UI")]
    public static void SetupResultUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found. Please run Wave UI setup or Restart UI setup first to create a Canvas.");
            return;
        }

        GameResultUI existing = FindFirstObjectByType<GameResultUI>();
        if (existing != null)
        {
            Debug.LogWarning("GameResultUI already exists.");
            return;
        }

        GameObject bgObj = new GameObject("GameResultUI");
        bgObj.transform.SetParent(canvas.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f); // Dark semi-transparent
        bgImage.raycastTarget = true; // Block clicks if needed

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bgObj.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; 
        rect.sizeDelta = new Vector2(800f, 200f);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 72;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.richText = true;

        GameResultUI uiScript = bgObj.AddComponent<GameResultUI>();
        // Using reflection or SerializedObject to assign resultText since it's private
        SerializedObject soUI = new SerializedObject(uiScript);
        soUI.FindProperty("resultText").objectReferenceValue = text;
        soUI.ApplyModifiedProperties();

        bgObj.SetActive(false); // Hidden by default, script needs to handle turning it on if we hide the root

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        Debug.Log("Successfully created Game Result UI at the center of the Canvas!");
    }
}
