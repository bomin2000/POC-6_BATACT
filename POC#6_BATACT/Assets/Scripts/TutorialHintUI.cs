using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialHintUI : MonoBehaviour
{
    public static TutorialHintUI Instance { get; private set; }

    private TextMeshProUGUI controlsText;
    private TextMeshProUGUI tutorialHintText;
    private CanvasGroup tutorialHintCanvasGroup;
    
    private TextMeshProUGUI weaponHintText;
    private CanvasGroup weaponHintCanvasGroup;
    
    private string currentWeaponHint = "";
    private bool showWeaponHint = true;
    
    private string currentTutorialHint = "";
    private float tutorialHintTimer = 0f;

    [Header("Controls UI Settings")]
    [SerializeField] private Vector2 controlsPosition = new Vector2(-10f, -20f);
    [SerializeField] private Vector2 controlsSize = new Vector2(250f, 200f);
    [SerializeField] private float controlsFontSize = 26f;

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
        controlsRect.anchoredPosition = controlsPosition;
        controlsRect.sizeDelta = controlsSize;

        controlsText.fontSize = controlsFontSize;
        controlsText.color = new Color(1, 1, 1, 0.8f);
        controlsText.alignment = TextAlignmentOptions.TopLeft;
        controlsText.text = "<b><size=120%>조작 방법</size></b>\n<size=80%>이동:</size> W/A/S/D\n<size=80%>점프:</size> Space\n<size=80%>대시:</size> Shift\n<size=80%>기본 공격:</size> 좌클릭\n<size=80%>연계 스킬:</size> 우클릭\n<size=80%>무기 변환:</size> 마우스 휠\n<size=80%>무기 설명 On/Off:</size> Tab";
        controlsText.enableWordWrapping = false;
        
        // Add outline to make it readable
        controlsText.fontMaterial.EnableKeyword("OUTLINE_ON");
        controlsText.outlineWidth = 0.2f;
        controlsText.outlineColor = Color.black;

        // 3. Setup Tutorial Hint Text (Bottom Center)
        GameObject tutHintObj = new GameObject("TutorialHintText");
        tutHintObj.transform.SetParent(transform, false);
        tutorialHintCanvasGroup = tutHintObj.AddComponent<CanvasGroup>();
        tutorialHintCanvasGroup.alpha = 0f;

        tutorialHintText = tutHintObj.AddComponent<TextMeshProUGUI>();
        
        RectTransform tutHintRect = tutorialHintText.rectTransform;
        tutHintRect.anchorMin = new Vector2(0.5f, 0.2f);
        tutHintRect.anchorMax = new Vector2(0.5f, 0.2f);
        tutHintRect.pivot = new Vector2(0.5f, 0.5f);
        tutHintRect.anchoredPosition = new Vector2(0, 0);
        tutHintRect.sizeDelta = new Vector2(1200, 200);

        tutorialHintText.fontSize = 42;
        tutorialHintText.color = Color.yellow;
        tutorialHintText.alignment = TextAlignmentOptions.Center;
        tutorialHintText.text = "";
        
        tutorialHintText.fontMaterial.EnableKeyword("OUTLINE_ON");
        tutorialHintText.outlineWidth = 0.2f;
        tutorialHintText.outlineColor = Color.black;

        // 4. Setup Weapon Hint Text (Lower than Tutorial Hint)
        GameObject wepHintObj = new GameObject("WeaponHintText");
        wepHintObj.transform.SetParent(transform, false);
        weaponHintCanvasGroup = wepHintObj.AddComponent<CanvasGroup>();
        weaponHintCanvasGroup.alpha = 0f;

        weaponHintText = wepHintObj.AddComponent<TextMeshProUGUI>();
        
        RectTransform wepHintRect = weaponHintText.rectTransform;
        wepHintRect.anchorMin = new Vector2(0.5f, 0.2f);
        wepHintRect.anchorMax = new Vector2(0.5f, 0.2f);
        wepHintRect.pivot = new Vector2(0.5f, 0.5f);
        wepHintRect.anchoredPosition = new Vector2(0, -100); // 100 lower
        wepHintRect.sizeDelta = new Vector2(1200, 200);

        weaponHintText.fontSize = 30; // Font size 30
        weaponHintText.color = new Color(0.9f, 0.9f, 1f, 1f); // Slightly blueish white
        weaponHintText.alignment = TextAlignmentOptions.Center;
        weaponHintText.text = "";
        
        weaponHintText.fontMaterial.EnableKeyword("OUTLINE_ON");
        weaponHintText.outlineWidth = 0.2f;
        weaponHintText.outlineColor = Color.black;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showWeaponHint = !showWeaponHint;
        }

        UpdateHintDisplay();
    }

    private void UpdateHintDisplay()
    {
        // 1. Tutorial Hint
        if (!string.IsNullOrEmpty(currentTutorialHint))
        {
            tutorialHintText.text = currentTutorialHint;
            tutorialHintCanvasGroup.alpha = Mathf.MoveTowards(tutorialHintCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 5f);
        }
        else
        {
            tutorialHintCanvasGroup.alpha = Mathf.MoveTowards(tutorialHintCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * 5f);
        }

        // 2. Weapon Hint
        if (showWeaponHint && !string.IsNullOrEmpty(currentWeaponHint))
        {
            weaponHintText.text = currentWeaponHint;
            weaponHintCanvasGroup.alpha = Mathf.MoveTowards(weaponHintCanvasGroup.alpha, 1f, Time.unscaledDeltaTime * 5f);
        }
        else
        {
            weaponHintCanvasGroup.alpha = Mathf.MoveTowards(weaponHintCanvasGroup.alpha, 0f, Time.unscaledDeltaTime * 5f);
        }
    }

    public void ShowTutorialHint(string text)
    {
        currentTutorialHint = text;
    }

    public void HideTutorialHint(string text)
    {
        if (currentTutorialHint == text)
        {
            currentTutorialHint = "";
        }
    }

    public void ShowWeaponHint(string text)
    {
        currentWeaponHint = text;
    }
}
