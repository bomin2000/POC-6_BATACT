using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialHintUI : MonoBehaviour
{
    public static TutorialHintUI Instance { get; private set; }

    private TextMeshProUGUI controlsText;
    private TextMeshProUGUI hintText;
    private CanvasGroup hintCanvasGroup;
    private Coroutine hintCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("TutorialHintUI");
            go.AddComponent<TutorialHintUI>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupUI();
    }

    private void SetupUI()
    {
        // 1. Setup Canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Render on top of everything

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        gameObject.AddComponent<GraphicRaycaster>();

        // 2. Setup Controls Text (Top Left / Right)
        GameObject controlsObj = new GameObject("ControlsSummary");
        controlsObj.transform.SetParent(transform, false);
        controlsText = controlsObj.AddComponent<TextMeshProUGUI>();
        
        RectTransform controlsRect = controlsText.rectTransform;
        controlsRect.anchorMin = new Vector2(1, 1); // Top Right
        controlsRect.anchorMax = new Vector2(1, 1);
        controlsRect.pivot = new Vector2(1, 1);
        controlsRect.anchoredPosition = new Vector2(-10, -20);
        controlsRect.sizeDelta = new Vector2(250, 200);

        controlsText.fontSize = 26;
        controlsText.color = new Color(1, 1, 1, 0.8f);
        controlsText.alignment = TextAlignmentOptions.TopLeft;
        controlsText.text = "<b><size=120%>조작 방법</size></b>\n<size=80%>이동:</size> W/A/S/D\n<size=80%>점프:</size> Space\n<size=80%>대시:</size> Shift\n<size=80%>기본 공격:</size> 좌클릭\n<size=80%>연계 스킬:</size> 우클릭\n<size=80%>무기 변환:</size> 마우스 휠";
        controlsText.enableWordWrapping = false;
        
        // Add outline to make it readable
        controlsText.fontMaterial.EnableKeyword("OUTLINE_ON");
        controlsText.outlineWidth = 0.2f;
        controlsText.outlineColor = Color.black;

        // 3. Setup Hint Text (Bottom Center)
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(transform, false);
        hintCanvasGroup = hintObj.AddComponent<CanvasGroup>();
        hintCanvasGroup.alpha = 0f; // Hidden by default

        hintText = hintObj.AddComponent<TextMeshProUGUI>();
        
        RectTransform hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0.2f); // Bottom center
        hintRect.anchorMax = new Vector2(0.5f, 0.2f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0, 0);
        hintRect.sizeDelta = new Vector2(1200, 200);

        hintText.fontSize = 42;
        hintText.color = Color.yellow;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.text = "";
        
        hintText.fontMaterial.EnableKeyword("OUTLINE_ON");
        hintText.outlineWidth = 0.2f;
        hintText.outlineColor = Color.black;
    }

    public void ShowHint(string text, float duration = 4f)
    {
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
        }
        
        hintText.text = text;
        hintCoroutine = StartCoroutine(HintRoutine(duration));
    }

    private IEnumerator HintRoutine(float duration)
    {
        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            hintCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
            yield return null;
        }
        hintCanvasGroup.alpha = 1f;

        // Wait
        yield return new WaitForSecondsRealtime(duration);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            hintCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.5f);
            yield return null;
        }
        hintCanvasGroup.alpha = 0f;
        hintCoroutine = null;
    }
}
