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
    private WeaponState? previousState = null;
    private bool spearHintShown = false;
    private bool boomerangHintShown = false;
    private bool scissorsHintShown = false;

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
        
        if (previousState != state)
        {
            if (previousState.HasValue && TutorialHintUI.Instance != null)
            {
                ShowWeaponHint(state);
            }
            previousState = state;
        }

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
                return "창";
            case WeaponState.Boomerang:
                return "부메랑";
            case WeaponState.Scissors:
                return "가위";
            case WeaponState.BareHand:
                return "맨손";
            default:
                return state.ToString();
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

    private void ShowWeaponHint(WeaponState state)
    {
        string hint = "";
        switch (state)
        {
            case WeaponState.Spear:
                if (!spearHintShown)
                {
                    hint = "창은 긴 사거리와 밀어내기에 강합니다.\n벽에 닿으면 이동이 제한됩니다.";
                    spearHintShown = true;
                }
                break;
            case WeaponState.Boomerang:
                if (!boomerangHintShown)
                {
                    hint = "부메랑은 적을 끌어오고, Anchor에 걸어 이동할 수 있습니다.";
                    boomerangHintShown = true;
                }
                break;
            case WeaponState.Scissors:
                if (!scissorsHintShown)
                {
                    hint = "가위는 근거리와 적 부위파괴에 강합니다.\n벽에서 공격을 누르면 천천히 내려옵니다.";
                    scissorsHintShown = true;
                }
                break;
        }

        if (!string.IsNullOrEmpty(hint))
        {
            TutorialHintUI.Instance.ShowHint(hint, 4f);
        }
    }
}
