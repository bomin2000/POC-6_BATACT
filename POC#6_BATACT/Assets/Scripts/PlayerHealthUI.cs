using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthLabel;

    [Header("Auto Build")]
    [SerializeField] private bool buildHudIfMissing = true;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(32f, -32f);
    [SerializeField] private Vector2 barSize = new Vector2(280f, 22f);
    [SerializeField] private string titleText = "HP";

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.95f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color lowHealthFillColor = new Color(1f, 0.75f, 0.12f, 1f);
    [SerializeField] private Color labelColor = Color.white;

    [Header("Behavior")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private float lerpSharpness = 18f;
    [SerializeField] private float lowHealthThreshold = 0.35f;

    private float targetValue = 1f;
    private Image fillImage;

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

        if (healthSlider == null && buildHudIfMissing)
        {
            BuildHud();
        }

        if (autoFindPlayer && playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.transition = Selectable.Transition.None;
        }

        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged.AddListener(OnHealthChanged);
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    private void Update()
    {
        if (healthSlider == null)
        {
            return;
        }

        float t = 1f - Mathf.Exp(-lerpSharpness * Time.unscaledDeltaTime);
        healthSlider.value = Mathf.Lerp(healthSlider.value, targetValue, t);

        if (fillImage != null)
        {
            Color targetColor = targetValue <= lowHealthThreshold ? lowHealthFillColor : fillColor;
            fillImage.color = Color.Lerp(fillImage.color, targetColor, t);
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        targetValue = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (healthLabel != null)
        {
            healthLabel.text = $"{titleText} {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }

    private void BuildHud()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("PlayerHUDCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObject.transform, false);
        }

        RectTransform root = GetComponent<RectTransform>();
        if (root == null)
        {
            root = gameObject.AddComponent<RectTransform>();
        }

        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = new Vector2(barSize.x, barSize.y + 26f);

        GameObject labelObject = new GameObject("HealthLabel");
        labelObject.transform.SetParent(transform, false);
        healthLabel = labelObject.AddComponent<TextMeshProUGUI>();
        healthLabel.text = $"{titleText} 100 / 100";
        healthLabel.fontSize = 18f;
        healthLabel.color = labelColor;
        healthLabel.alignment = TextAlignmentOptions.Left;

        RectTransform labelRect = healthLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(0f, 24f);

        GameObject sliderObject = new GameObject("PlayerHealthSlider");
        sliderObject.transform.SetParent(transform, false);
        healthSlider = sliderObject.AddComponent<Slider>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = 1f;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.targetGraphic = null;

        RectTransform sliderRect = healthSlider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(0f, -26f);
        sliderRect.sizeDelta = barSize;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(backgroundObject.transform, false);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        healthSlider.fillRect = fillRect;
    }
}
