using System.Collections;
using UnityEngine;

public sealed class DualBladeWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform upperBlade;
    [SerializeField] private Transform lowerBlade;
    [SerializeField] private Transform throwSpawnPoint;
    [SerializeField] private WeaponHitbox2D meleeHitbox;
    [SerializeField] private DualBladeBoomerangProjectile boomerangPrefab;

    [Header("Hitbox Profiles")]
    [SerializeField] private WeaponHitboxProfile spearProfile;
    [SerializeField] private WeaponHitboxProfile boomerangProfile;
    [SerializeField] private WeaponHitboxProfile scissorsProfile;

    [Header("Visual Debug")]
    [SerializeField] private bool forceWeaponRenderInFront = true;
    [SerializeField] private int weaponSortingOrder = 20;

    [Header("Pivot Anchors")]
    [SerializeField] private Vector3 spearPivotLocalPosition = new Vector3(0f, 0.45f, 0f);
    [SerializeField] private Vector3 boomerangPivotLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 scissorsPivotLocalPosition = new Vector3(0f, -0.45f, 0f);

    [Header("Form Poses")]
    [SerializeField] private BladePose spearPose = new BladePose(0f, 180f, 1.45f, 0.95f);
    [SerializeField] private BladePose boomerangPose = new BladePose(90f, 0f, 1f, 0f);
    [SerializeField] private BladePose scissorsPose = new BladePose(35f, -35f, 0.75f, 0.5f);
    [SerializeField] private float snapLerpSharpness = 40f;

    [Header("Input")]
    [SerializeField] private KeyCode boomerangSelectKey = KeyCode.Mouse2;
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
    [SerializeField] private bool allowNumberKeyMorph = true;
    [SerializeField] private float perfectCatchInputWindow = 0.12f;

    [Header("Melee Attack Animation")]
    [SerializeField] private float spearThrustDistance = 1.05f;
    [SerializeField] private float spearThrustOutSeconds = 0.055f;
    [SerializeField] private float spearThrustBackSeconds = 0.11f;
    [SerializeField] private float spearThrustStretch = 0.25f;
    [SerializeField] private float scissorsCutCloseAngle = 8f;
    [SerializeField] private float scissorsCutCloseSeconds = 0.07f;
    [SerializeField] private float scissorsCutOpenSeconds = 0.13f;
    [SerializeField] private float scissorsCutLungeDistance = 0.25f;

    public WeaponState CurrentState { get; private set; } = WeaponState.Boomerang;
    public WeaponState? QueuedState { get; private set; }
    public bool IsInputLocked => CurrentState == WeaponState.BareHand;

    private DualBladeBoomerangProjectile activeBoomerang;
    private WeaponState visibleForm = WeaponState.Boomerang;
    private Vector2 aimDirection = Vector2.right;
    private float lastCatchRelevantInputTime = -999f;
    private Coroutine meleeAnimationRoutine;
    private Vector3 attackPivotOffset;
    private float attackUpperAngleOffset;
    private float attackLowerAngleOffset;
    private float attackLengthScaleOffset;

    private void Awake()
    {
        UpgradeLegacyPoseDefaults();

        if (weaponRoot == null)
        {
            weaponRoot = transform;
        }

        if (throwSpawnPoint == null)
        {
            throwSpawnPoint = weaponRoot;
        }

        if (meleeHitbox != null)
        {
            meleeHitbox.Initialize(transform);
        }

        ConfigureWeaponRenderers();
        ApplyFormInstant(CurrentState);
    }

    private void OnValidate()
    {
        UpgradeLegacyPoseDefaults();
        ConfigureWeaponRenderers();
    }

    private void Update()
    {
        UpdateAimDirection();
        ReadFormInput();
        ReadAttackInput();
        SmoothSnapVisibleForm();
    }

    private void UpdateAimDirection()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            aimDirection = transform.right;
            return;
        }

        Vector3 mouseWorld = GetMouseWorldOnPlayerPlane(mainCamera);
        Vector2 toMouse = mouseWorld - transform.position;

        if (toMouse.sqrMagnitude > 0.0001f)
        {
            aimDirection = toMouse.normalized;
            weaponRoot.right = aimDirection;
        }
    }

    private Vector3 GetMouseWorldOnPlayerPlane(Camera mainCamera)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane playPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, transform.position.z));

        if (playPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        Vector3 fallback = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        fallback.z = transform.position.z;
        return fallback;
    }

    private void ReadFormInput()
    {
        float wheel = Input.mouseScrollDelta.y;
        WeaponState? requestedState = null;

        if (wheel > 0f)
        {
            requestedState = WeaponState.Spear;
        }
        else if (wheel < 0f)
        {
            requestedState = WeaponState.Scissors;
        }
        else if (Input.GetKeyDown(boomerangSelectKey))
        {
            requestedState = WeaponState.Boomerang;
        }

        if (allowNumberKeyMorph || Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                requestedState = WeaponState.Spear;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                requestedState = WeaponState.Boomerang;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                requestedState = WeaponState.Scissors;
            }
        }

        if (!requestedState.HasValue)
        {
            return;
        }

        lastCatchRelevantInputTime = Time.time;
        RequestMorph(requestedState.Value);
    }

    private void ReadAttackInput()
    {
        if (!Input.GetKeyDown(attackKey))
        {
            return;
        }

        lastCatchRelevantInputTime = Time.time;

        if (CurrentState == WeaponState.BareHand)
        {
            return;
        }

        switch (CurrentState)
        {
            case WeaponState.Spear:
                PlayMeleeVisualAnimation(WeaponState.Spear);
                TryStartMeleeHitbox(spearProfile, WeaponState.Spear);
                break;

            case WeaponState.Scissors:
                PlayMeleeVisualAnimation(WeaponState.Scissors);
                TryStartMeleeHitbox(scissorsProfile, WeaponState.Scissors);
                break;

            case WeaponState.Boomerang:
                ThrowBoomerang();
                break;
        }
    }

    private void TryStartMeleeHitbox(WeaponHitboxProfile profile, WeaponState attackState)
    {
        if (meleeHitbox == null || profile == null)
        {
            Debug.LogWarning($"DualBladeWeaponController: {attackState} attack is missing a hitbox or profile.");
            return;
        }

        if (!meleeHitbox.TryAttack(profile, aimDirection))
        {
            Debug.Log($"DualBladeWeaponController: {attackState} hitbox was skipped because another melee attack is still active.");
            return;
        }
    }

    private void PlayMeleeVisualAnimation(WeaponState attackState)
    {
        if (meleeAnimationRoutine != null)
        {
            StopCoroutine(meleeAnimationRoutine);
        }

        meleeAnimationRoutine = StartCoroutine(MeleeAnimationRoutine(attackState));
    }

    public void RequestMorph(WeaponState targetState)
    {
        if (targetState == WeaponState.BareHand)
        {
            return;
        }

        if (CurrentState == WeaponState.BareHand)
        {
            // 맨손 상태에서는 즉시 변형하지 않고, 캐치 프레임에 격발할 상태만 저장합니다.
            QueuedState = targetState;
            return;
        }

        TransitionTo(targetState);
    }

    private void ThrowBoomerang()
    {
        if (boomerangPrefab == null || activeBoomerang != null)
        {
            return;
        }

        activeBoomerang = Instantiate(boomerangPrefab, throwSpawnPoint.position, Quaternion.identity);
        activeBoomerang.Caught += OnBoomerangCaught;
        activeBoomerang.Launch(transform, boomerangProfile, aimDirection);

        TransitionTo(WeaponState.BareHand);
    }

    private void OnBoomerangCaught(DualBladeBoomerangProjectile projectile)
    {
        if (projectile != activeBoomerang)
        {
            return;
        }

        activeBoomerang.Caught -= OnBoomerangCaught;
        Destroy(activeBoomerang.gameObject);
        activeBoomerang = null;

        bool perfectCatch = Time.time - lastCatchRelevantInputTime <= perfectCatchInputWindow;
        if (perfectCatch)
        {
            Debug.Log("Perfect Catch: critical buff / dash cancel opportunity granted.");
        }

        WeaponState nextState = QueuedState.HasValue ? QueuedState.Value : WeaponState.Boomerang;
        QueuedState = null;
        TransitionTo(nextState);
    }

    private void TransitionTo(WeaponState nextState)
    {
        if (meleeAnimationRoutine != null)
        {
            StopCoroutine(meleeAnimationRoutine);
            meleeAnimationRoutine = null;
        }

        ClearAttackOffsets();
        CurrentState = nextState;

        if (nextState != WeaponState.BareHand)
        {
            visibleForm = nextState;
            SetWeaponVisible(true);
        }
        else
        {
            SetWeaponVisible(false);
        }
    }

    private void SetWeaponVisible(bool visible)
    {
        if (weaponRoot != null && weaponRoot != transform)
        {
            weaponRoot.gameObject.SetActive(visible);
            return;
        }

        if (pivot != null)
        {
            pivot.gameObject.SetActive(visible);
        }

        if (upperBlade != null)
        {
            upperBlade.gameObject.SetActive(visible);
        }

        if (lowerBlade != null)
        {
            lowerBlade.gameObject.SetActive(visible);
        }
    }

    private void SmoothSnapVisibleForm()
    {
        if (CurrentState == WeaponState.BareHand || pivot == null)
        {
            return;
        }

        Vector3 targetPivot = GetPivotPosition(visibleForm) + attackPivotOffset;
        BladePose pose = GetPose(visibleForm);
        pose.upperBladeAngle += attackUpperAngleOffset;
        pose.lowerBladeAngle += attackLowerAngleOffset;
        pose.lengthScale += attackLengthScaleOffset;
        float t = 1f - Mathf.Exp(-snapLerpSharpness * Time.deltaTime);

        pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPivot, t);
        ApplyBladePose(pose, t);
    }

    private void ApplyFormInstant(WeaponState state)
    {
        visibleForm = state == WeaponState.BareHand ? WeaponState.Boomerang : state;

        if (pivot != null)
        {
            pivot.localPosition = GetPivotPosition(visibleForm);
        }

        ApplyBladePose(GetPose(visibleForm), 1f);
        SetWeaponVisible(state != WeaponState.BareHand);
    }

    private Vector3 GetPivotPosition(WeaponState state)
    {
        switch (state)
        {
            case WeaponState.Spear:
                return spearPivotLocalPosition;
            case WeaponState.Scissors:
                return scissorsPivotLocalPosition;
            case WeaponState.Boomerang:
            default:
                return boomerangPivotLocalPosition;
        }
    }

    private BladePose GetPose(WeaponState state)
    {
        switch (state)
        {
            case WeaponState.Spear:
                return spearPose;
            case WeaponState.Scissors:
                return scissorsPose;
            case WeaponState.Boomerang:
            default:
                return boomerangPose;
        }
    }

    private void UpgradeLegacyPoseDefaults()
    {
        if (spearPose.bladeDistanceFromPivot <= 0f)
        {
            spearPose.bladeDistanceFromPivot = 0.95f;
        }

        if (Mathf.Approximately(boomerangPose.bladeDistanceFromPivot, 0.45f))
        {
            boomerangPose.bladeDistanceFromPivot = 0f;
        }

        if (scissorsPose.bladeDistanceFromPivot <= 0f)
        {
            scissorsPose.bladeDistanceFromPivot = 0.5f;
        }

        if (Mathf.Approximately(scissorsPose.upperBladeAngle, 35f) && Mathf.Approximately(scissorsPose.lowerBladeAngle, 145f))
        {
            scissorsPose.lowerBladeAngle = -35f;
        }
    }

    private void ConfigureWeaponRenderers()
    {
        if (!forceWeaponRenderInFront || weaponRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = weaponRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = weaponSortingOrder;
        }
    }

    private IEnumerator MeleeAnimationRoutine(WeaponState attackState)
    {
        ClearAttackOffsets();

        if (attackState == WeaponState.Spear)
        {
            yield return SpearThrustAnimation();
        }
        else if (attackState == WeaponState.Scissors)
        {
            yield return ScissorsCutAnimation();
        }

        ClearAttackOffsets();
        meleeAnimationRoutine = null;
    }

    private IEnumerator SpearThrustAnimation()
    {
        float elapsed = 0f;
        while (elapsed < spearThrustOutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / spearThrustOutSeconds));
            attackPivotOffset = Vector3.right * (spearThrustDistance * t);
            attackLengthScaleOffset = spearThrustStretch * t;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < spearThrustBackSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / spearThrustBackSeconds));
            attackPivotOffset = Vector3.right * Mathf.Lerp(spearThrustDistance, 0f, t);
            attackLengthScaleOffset = Mathf.Lerp(spearThrustStretch, 0f, t);
            yield return null;
        }
    }

    private IEnumerator ScissorsCutAnimation()
    {
        BladePose basePose = scissorsPose;
        float closeUpperOffset = scissorsCutCloseAngle - basePose.upperBladeAngle;
        float closeLowerOffset = -scissorsCutCloseAngle - basePose.lowerBladeAngle;

        float elapsed = 0f;
        while (elapsed < scissorsCutCloseSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / scissorsCutCloseSeconds));
            attackUpperAngleOffset = closeUpperOffset * t;
            attackLowerAngleOffset = closeLowerOffset * t;
            attackPivotOffset = Vector3.right * (scissorsCutLungeDistance * t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < scissorsCutOpenSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / scissorsCutOpenSeconds));
            float remaining = 1f - t;
            attackUpperAngleOffset = closeUpperOffset * remaining;
            attackLowerAngleOffset = closeLowerOffset * remaining;
            attackPivotOffset = Vector3.right * (scissorsCutLungeDistance * remaining);
            yield return null;
        }
    }

    private void ClearAttackOffsets()
    {
        attackPivotOffset = Vector3.zero;
        attackUpperAngleOffset = 0f;
        attackLowerAngleOffset = 0f;
        attackLengthScaleOffset = 0f;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void ApplyBladePose(BladePose pose, float t)
    {
        float bladeDistance = Mathf.Max(0f, pose.bladeDistanceFromPivot);

        if (upperBlade != null)
        {
            Vector3 upperDirection = GetLocalDirection(pose.upperBladeAngle);
            upperBlade.localPosition = Vector3.Lerp(upperBlade.localPosition, upperDirection * bladeDistance, t);
            upperBlade.localRotation = Quaternion.Lerp(
                upperBlade.localRotation,
                Quaternion.Euler(0f, 0f, pose.upperBladeAngle),
                t);
            upperBlade.localScale = Vector3.Lerp(upperBlade.localScale, new Vector3(pose.lengthScale, 1f, 1f), t);
        }

        if (lowerBlade != null)
        {
            Vector3 lowerDirection = GetLocalDirection(pose.lowerBladeAngle);
            lowerBlade.localPosition = Vector3.Lerp(lowerBlade.localPosition, lowerDirection * bladeDistance, t);
            lowerBlade.localRotation = Quaternion.Lerp(
                lowerBlade.localRotation,
                Quaternion.Euler(0f, 0f, pose.lowerBladeAngle),
                t);
            lowerBlade.localScale = Vector3.Lerp(lowerBlade.localScale, new Vector3(pose.lengthScale, 1f, 1f), t);
        }
    }

    private static Vector3 GetLocalDirection(float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }

    private static float GetFallbackBladeDistance(float lengthScale)
    {
        // 기존 씬에는 새 필드가 0으로 들어갈 수 있으므로, 구버전 세팅도 바로 보이게 안전값을 씁니다.
        return Mathf.Max(0.35f, 0.65f * Mathf.Max(0.5f, lengthScale));
    }
}

[System.Serializable]
public struct BladePose
{
    public float upperBladeAngle;
    public float lowerBladeAngle;
    public float lengthScale;
    public float bladeDistanceFromPivot;

    public BladePose(float upperBladeAngle, float lowerBladeAngle, float lengthScale, float bladeDistanceFromPivot)
    {
        this.upperBladeAngle = upperBladeAngle;
        this.lowerBladeAngle = lowerBladeAngle;
        this.lengthScale = lengthScale;
        this.bladeDistanceFromPivot = bladeDistanceFromPivot;
    }
}
