using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class WeaponModeSliderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DualBladeWeaponController weaponController;
    [SerializeField] private Slider modeSlider;
    [SerializeField] private TextMeshProUGUI modeLabel;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color spearColor = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private Color boomerangColor = new Color(0.2f, 0.35f, 1f);
    [SerializeField] private Color scissorsColor = new Color(0.8f, 0.2f, 1f);
    [SerializeField] private Color bareHandColor = Color.gray;

    [Header("Tuning")]
    [SerializeField] private bool autoFindWeaponController = true;
    [SerializeField] private float sliderLerpSharpness = 18f;

    private float targetValue = 1f;

    private void Awake()
    {
        if (modeSlider == null)
        {
            modeSlider = GetComponentInChildren<Slider>();
        }

        if (autoFindWeaponController && weaponController == null)
        {
            weaponController = FindFirstObjectByType<DualBladeWeaponController>();
        }

        if (modeSlider != null)
        {
            modeSlider.minValue = 0f;
            modeSlider.maxValue = 2f;
            modeSlider.wholeNumbers = false;
        }
    }

    private void Update()
    {
        if (weaponController == null || modeSlider == null)
        {
            return;
        }

        WeaponState state = weaponController.CurrentState;
        targetValue = GetSliderValue(state);

        float t = 1f - Mathf.Exp(-sliderLerpSharpness * Time.unscaledDeltaTime);
        modeSlider.value = Mathf.Lerp(modeSlider.value, targetValue, t);

        if (modeLabel != null)
        {
            modeLabel.text = GetLabel(state);
        }

        if (fillImage != null)
        {
            fillImage.color = GetColor(state);
        }
    }

    private float GetSliderValue(WeaponState state)
    {
        switch (state)
        {
            case WeaponState.Spear:
                return 0f;
            case WeaponState.Boomerang:
            case WeaponState.BareHand:
                return 1f;
            case WeaponState.Scissors:
                return 2f;
            default:
                return 1f;
        }
    }

    private string GetLabel(WeaponState state)
    {
        switch (state)
        {
            case WeaponState.Spear:
                return "SPEAR";
            case WeaponState.Boomerang:
                return "BOOMERANG";
            case WeaponState.Scissors:
                return "SCISSORS";
            case WeaponState.BareHand:
                return "BARE HAND";
            default:
                return state.ToString().ToUpperInvariant();
        }
    }

    private Color GetColor(WeaponState state)
    {
        switch (state)
        {
            case WeaponState.Spear:
                return spearColor;
            case WeaponState.Boomerang:
                return boomerangColor;
            case WeaponState.Scissors:
                return scissorsColor;
            case WeaponState.BareHand:
                return bareHandColor;
            default:
                return Color.white;
        }
    }
}
