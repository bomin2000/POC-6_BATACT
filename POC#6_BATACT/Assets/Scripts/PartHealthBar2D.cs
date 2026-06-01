using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyPart))]
public sealed class PartHealthBar2D : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private Vector2 size = new Vector2(0.8f, 0.1f);
    [SerializeField] private float worldScale = 0.015f;
    [SerializeField] private bool faceCamera = true;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool hideOnDeath = true;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue for shield

    private EnemyPart receiver;
    private Canvas canvas;
    private Slider slider;
    private Image fillImage;
    private Camera mainCamera;

    private void Awake()
    {
        receiver = GetComponent<EnemyPart>();
        mainCamera = Camera.main;
        BuildHealthBar();
    }

    private void OnEnable()
    {
        if (receiver != null)
        {
            receiver.HealthChanged.AddListener(OnHealthChanged);
            receiver.OnBroken.AddListener(OnDied);
            // Default 100% since we don't have GetCurrentHealth yet, but it will update on hit
            OnHealthChanged(1f, 1f); 
        }
    }

    private void OnDisable()
    {
        if (receiver != null)
        {
            receiver.HealthChanged.RemoveListener(OnHealthChanged);
            receiver.OnBroken.RemoveListener(OnDied);
        }
    }

    private void LateUpdate()
    {
        if (canvas == null) return;

        canvas.transform.position = transform.position + worldOffset;

        if (faceCamera)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                canvas.transform.rotation = mainCamera.transform.rotation;
            }
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (slider == null) return;

        float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        slider.value = normalized;

        if (hideWhenFull && canvas != null)
        {
            canvas.gameObject.SetActive(normalized < 0.999f && normalized > 0f);
        }
        else if (canvas != null && !receiver.IsBroken)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private void OnDied()
    {
        if (hideOnDeath && canvas != null)
        {
            canvas.gameObject.SetActive(false);
        }
    }

    private void BuildHealthBar()
    {
        GameObject canvasObject = new GameObject("PartHealthBar");
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = worldOffset;
        canvasObject.transform.localScale = Vector3.one * worldScale;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size * 100f;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = backgroundColor;
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(backgroundObject.transform, false);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRect = fillImage.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        slider = canvasObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.transition = Selectable.Transition.None;
        slider.targetGraphic = null;
        slider.fillRect = fillRect;
        slider.direction = Slider.Direction.LeftToRight;
        
        if (hideWhenFull)
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
