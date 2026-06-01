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
    private WeaponState previousState = WeaponState.BareHand;
    private System.Collections.Generic.Dictionary<WeaponState, int> hintShowCounts = new System.Collections.Generic.Dictionary<WeaponState, int>();

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
            if (TutorialHintUI.Instance != null)
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

    private void ShowWeaponHint(WeaponState newState)
    {
        string hint = "";
        
        if (!hintShowCounts.ContainsKey(newState))
        {
            hintShowCounts[newState] = 0;
        }
        
        if (hintShowCounts[newState] < 2)
        {
            switch (newState)
            {
                case WeaponState.Spear:
                    hint = "창은 리치가 길며 공격속도가 느립니다.\n점프 중 벽을 향해 좌클릭시 장대점프가 가능합니다.";
                    break;
                case WeaponState.Boomerang:
                    hint = "부메랑은 중거리 투척 무기입니다.\n우클릭 시 부메랑 위치로 로프를 타고 날아갑니다.";
                    break;
                case WeaponState.Scissors:
                    hint = "가위는 근거리와 적 부위파괴에 강합니다.\n벽에서 공격을 누르면 천천히 내려옵니다.";
                    break;
            }
            hintShowCounts[newState]++;
        }

        if (TutorialHintUI.Instance != null)
        {
            TutorialHintUI.Instance.ShowWeaponHint(hint);
        }
    }
}
