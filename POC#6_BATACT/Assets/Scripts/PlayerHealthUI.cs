using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Text healthLabel;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private float lerpSharpness = 18f;

    private float targetValue = 1f;

    private void Awake()
    {
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
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
    }

    private void OnHealthChanged(float current, float max)
    {
        targetValue = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (healthLabel != null)
        {
            healthLabel.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
}
