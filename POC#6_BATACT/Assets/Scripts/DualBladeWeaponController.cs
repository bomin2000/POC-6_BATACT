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

    public WeaponHitboxProfile SpearProfile => spearProfile;
    public WeaponHitboxProfile BoomerangProfile => boomerangProfile;
    public WeaponHitboxProfile ScissorsProfile => scissorsProfile;

    [Header("Visual Debug")]
    [SerializeField] private bool forceWeaponRenderInFront = true;
    [SerializeField] private int weaponSortingOrder = 20;

    [Header("Physics Safety")]
    [SerializeField] private bool forceBladeCollidersAsTriggers = true;

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
    [SerializeField] private bool wheelCyclesThroughBoomerang = true;
    [SerializeField] private bool allowNumberKeyMorph = true;
    [SerializeField] private float perfectCatchInputWindow = 0.12f;
    [SerializeField] private float catchStabilizeSeconds = 0.12f;

    [Header("Side View Aiming")]
    [SerializeField] private bool sideViewMirrorAim = true;
    [SerializeField] private bool drivePlayerFacingFromAim = true;
    [SerializeField] private float sideViewAimMaxAngle = 80f;

    [Header("Melee Attack Animation")]
    [SerializeField] private float spearThrustDistance = 1.05f;
    [SerializeField] private float spearThrustOutSeconds = 0.055f;
    [SerializeField] private float spearThrustBackSeconds = 0.11f;
    [SerializeField] private float spearThrustStretch = 0.25f;
    [SerializeField] private float scissorsCutCloseAngle = 8f;
    [SerializeField] private float scissorsCutCloseSeconds = 0.07f;
    [SerializeField] private float scissorsCutOpenSeconds = 0.13f;
    [SerializeField] private float scissorsCutLungeDistance = 0.25f;

    [Header("Combo Buffer")]
    [SerializeField] private bool enableComboBuffer = true;
    [SerializeField] private float comboResetSeconds = 0.7f;
    [SerializeField] private int spearComboCount = 3;
    [SerializeField] private int scissorsComboCount = 2;
    [SerializeField] private float bufferedInputWindow = 0.22f;
    [SerializeField] private float[] spearThrustDistanceMultipliers = { 1f, 1.15f, 1.35f };
    [SerializeField] private float[] spearStretchMultipliers = { 1f, 1.1f, 1.25f };
    [SerializeField] private float[] scissorsLungeMultipliers = { 1f, 1.25f };
    [SerializeField] private float[] scissorsCloseAngleByCombo = { 8f, 0f };

    public WeaponState CurrentState { get; private set; } = WeaponState.Boomerang;
    public WeaponState? QueuedState { get; private set; }
    public bool IsInputLocked => CurrentState == WeaponState.BareHand;
    public Transform WeaponRoot => weaponRoot;

    public bool ContainsWeaponCollider(Collider2D candidate)
    {
        if (candidate == null || weaponRoot == null)
        {
            return false;
        }

        return candidate.transform == weaponRoot || candidate.transform.IsChildOf(weaponRoot);
    }

    private DualBladeBoomerangProjectile activeBoomerang;
    private WeaponState visibleForm = WeaponState.Boomerang;
    private Vector2 aimDirection = Vector2.right;
    private float lastCatchRelevantInputTime = -999f;
    private Coroutine meleeAnimationRoutine;
    private Vector3 attackPivotOffset;
    private float attackUpperAngleOffset;
    private float attackLowerAngleOffset;
    private float attackLengthScaleOffset;
    private WeaponState comboState = WeaponState.Boomerang;
    private int comboIndex;
    private float lastComboTime = -999f;
    private bool hasBufferedAttack;
    private float bufferedAttackTime;
    private PlayerTopDownMovement playerMovement;

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

        playerMovement = GetComponent<PlayerTopDownMovement>();
        ConfigureWeaponRenderers();
        ConfigureBladeColliders();
        ApplyFormInstant(CurrentState);
    }

    private void OnValidate()
    {
        UpgradeLegacyPoseDefaults();
        ConfigureWeaponRenderers();
        ConfigureBladeColliders();
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
            if (sideViewMirrorAim)
            {
                int aimSign = toMouse.x >= 0f ? 1 : -1;
                aimDirection = ClampAimToFacingHemisphere(toMouse.normalized, aimSign);
                ApplySideViewWeaponFacing(aimSign, aimDirection);
            }
            else
            {
                aimDirection = toMouse.normalized;
                weaponRoot.right = aimDirection;
            }
        }
    }

    private Vector2 ClampAimToFacingHemisphere(Vector2 rawAim, int aimSign)
    {
        float localX = Mathf.Abs(rawAim.x);
        float localY = rawAim.y;
        float localAngle = Mathf.Atan2(localY, Mathf.Max(0.001f, localX)) * Mathf.Rad2Deg;
        localAngle = Mathf.Clamp(localAngle, -sideViewAimMaxAngle, sideViewAimMaxAngle);

        float radians = localAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians) * aimSign, Mathf.Sin(radians)).normalized;
    }

    private void ApplySideViewWeaponFacing(int aimSign, Vector2 worldAim)
    {
        if (weaponRoot != null)
        {
            float localAngle = Mathf.Atan2(worldAim.y, Mathf.Abs(worldAim.x)) * Mathf.Rad2Deg;
            float visualAngle = aimSign > 0 ? localAngle : -localAngle;
            weaponRoot.localRotation = Quaternion.Euler(0f, 0f, visualAngle);

            Vector3 scale = weaponRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * aimSign;
            scale.y = Mathf.Abs(scale.y);
            weaponRoot.localScale = scale;
        }

        if (drivePlayerFacingFromAim)
        {
            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerTopDownMovement>();
            }

            if (playerMovement != null)
            {
                playerMovement.SetAimFacingSign(aimSign);
            }
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
            requestedState = wheelCyclesThroughBoomerang ? StepWheelState(1) : WeaponState.Spear;
        }
        else if (wheel < 0f)
        {
            requestedState = wheelCyclesThroughBoomerang ? StepWheelState(-1) : WeaponState.Scissors;
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

    private WeaponState StepWheelState(int direction)
    {
        WeaponState baseState = QueuedState.HasValue ? QueuedState.Value : CurrentState;

        if (baseState == WeaponState.BareHand)
        {
            baseState = visibleForm;
        }

        if (direction > 0)
        {
            switch (baseState)
            {
                case WeaponState.Scissors:
                    return WeaponState.Boomerang;
                case WeaponState.Boomerang:
                    return WeaponState.Spear;
                case WeaponState.Spear:
                default:
                    return WeaponState.Spear;
            }
        }

        switch (baseState)
        {
            case WeaponState.Spear:
                return WeaponState.Boomerang;
            case WeaponState.Boomerang:
                return WeaponState.Scissors;
            case WeaponState.Scissors:
            default:
                return WeaponState.Scissors;
        }
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
                TryStartComboMeleeAttack(WeaponState.Spear, spearProfile);
                break;

            case WeaponState.Scissors:
                TryStartComboMeleeAttack(WeaponState.Scissors, scissorsProfile);
                break;

            case WeaponState.Boomerang:
                ThrowBoomerang();
                break;
        }
    }

    private void TryStartComboMeleeAttack(WeaponState attackState, WeaponHitboxProfile profile)
    {
        if (enableComboBuffer && meleeAnimationRoutine != null)
        {
            hasBufferedAttack = true;
            bufferedAttackTime = Time.time;
            return;
        }

        int nextComboIndex = ResolveNextComboIndex(attackState);
        StartMeleeAttack(attackState, profile, nextComboIndex);
    }

    private int ResolveNextComboIndex(WeaponState attackState)
    {
        int maxCombo = GetMaxComboCount(attackState);

        if (comboState != attackState || Time.time - lastComboTime > comboResetSeconds)
        {
            comboState = attackState;
            comboIndex = 0;
            return comboIndex;
        }

        comboIndex = (comboIndex + 1) % Mathf.Max(1, maxCombo);
        return comboIndex;
    }

    private int GetMaxComboCount(WeaponState attackState)
    {
        if (attackState == WeaponState.Spear)
        {
            return Mathf.Max(1, spearComboCount);
        }

        if (attackState == WeaponState.Scissors)
        {
            return Mathf.Max(1, scissorsComboCount);
        }

        return 1;
    }

    private void StartMeleeAttack(WeaponState attackState, WeaponHitboxProfile profile, int attackComboIndex)
    {
        PlayMeleeVisualAnimation(attackState, attackComboIndex);
        TryStartMeleeHitbox(profile, attackState);
        lastComboTime = Time.time;
    }

    private void TryStartMeleeHitbox(WeaponHitboxProfile profile, WeaponState attackState)
    {
        if (meleeHitbox == null || profile == null)
        {
            Debug.LogWarning($"DualBladeWeaponController: {attackState} attack is missing a hitbox or profile.");
            return;
        }

        if (!meleeHitbox.TryAttack(profile, aimDirection, GetAttackAnimationWorldOffset, true))
        {
            Debug.Log($"DualBladeWeaponController: {attackState} hitbox was skipped because another melee attack is still active.");
            return;
        }
    }

    private Vector2 GetAttackAnimationWorldOffset()
    {
        if (weaponRoot == null)
        {
            return attackPivotOffset;
        }

        return weaponRoot.TransformVector(attackPivotOffset);
    }

    private void PlayMeleeVisualAnimation(WeaponState attackState, int attackComboIndex)
    {
        if (meleeAnimationRoutine != null)
        {
            StopCoroutine(meleeAnimationRoutine);
        }

        meleeAnimationRoutine = StartCoroutine(MeleeAnimationRoutine(attackState, attackComboIndex));
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

        PlayerTopDownMovement movement = GetComponent<PlayerTopDownMovement>();
        if (movement != null)
        {
            movement.StabilizeAfterBoomerangCatch(catchStabilizeSeconds);
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
        hasBufferedAttack = false;
        comboState = nextState;
        comboIndex = 0;
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

    private void ConfigureBladeColliders()
    {
        if (!forceBladeCollidersAsTriggers || weaponRoot == null)
        {
            return;
        }

        Collider2D[] colliders = weaponRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }
    }

    private IEnumerator MeleeAnimationRoutine(WeaponState attackState, int attackComboIndex)
    {
        ClearAttackOffsets();

        if (attackState == WeaponState.Spear)
        {
            yield return SpearThrustAnimation(attackComboIndex);
        }
        else if (attackState == WeaponState.Scissors)
        {
            yield return ScissorsCutAnimation(attackComboIndex);
        }

        ClearAttackOffsets();
        meleeAnimationRoutine = null;
        TryConsumeBufferedAttack(attackState);
    }

    private void TryConsumeBufferedAttack(WeaponState attackState)
    {
        if (!enableComboBuffer || !hasBufferedAttack)
        {
            hasBufferedAttack = false;
            return;
        }

        bool isFresh = Time.time - bufferedAttackTime <= bufferedInputWindow;
        hasBufferedAttack = false;

        if (!isFresh || CurrentState != attackState)
        {
            return;
        }

        WeaponHitboxProfile profile = attackState == WeaponState.Spear ? spearProfile : scissorsProfile;
        int nextComboIndex = ResolveNextComboIndex(attackState);
        StartMeleeAttack(attackState, profile, nextComboIndex);
    }

    private IEnumerator SpearThrustAnimation(int attackComboIndex)
    {
        float distanceMultiplier = GetComboArrayValue(spearThrustDistanceMultipliers, attackComboIndex, 1f);
        float stretchMultiplier = GetComboArrayValue(spearStretchMultipliers, attackComboIndex, 1f);
        float thrustDistance = spearThrustDistance * distanceMultiplier;
        float thrustStretch = spearThrustStretch * stretchMultiplier;

        float elapsed = 0f;
        while (elapsed < spearThrustOutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / spearThrustOutSeconds));
            SetAttackPivotOffset(Vector3.right * (thrustDistance * t));
            attackLengthScaleOffset = thrustStretch * t;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < spearThrustBackSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / spearThrustBackSeconds));
            SetAttackPivotOffset(Vector3.right * Mathf.Lerp(thrustDistance, 0f, t));
            attackLengthScaleOffset = Mathf.Lerp(thrustStretch, 0f, t);
            yield return null;
        }
    }

    private IEnumerator ScissorsCutAnimation(int attackComboIndex)
    {
        BladePose basePose = scissorsPose;
        float closeAngle = GetComboArrayValue(scissorsCloseAngleByCombo, attackComboIndex, scissorsCutCloseAngle);
        float lungeDistance = scissorsCutLungeDistance * GetComboArrayValue(scissorsLungeMultipliers, attackComboIndex, 1f);
        float closeUpperOffset = closeAngle - basePose.upperBladeAngle;
        float closeLowerOffset = -closeAngle - basePose.lowerBladeAngle;

        float elapsed = 0f;
        while (elapsed < scissorsCutCloseSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / scissorsCutCloseSeconds));
            attackUpperAngleOffset = closeUpperOffset * t;
            attackLowerAngleOffset = closeLowerOffset * t;
            SetAttackPivotOffset(Vector3.right * (lungeDistance * t));
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
            SetAttackPivotOffset(Vector3.right * (lungeDistance * remaining));
            yield return null;
        }
    }

    private static float GetComboArrayValue(float[] values, int index, float fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        int safeIndex = Mathf.Clamp(index, 0, values.Length - 1);
        return values[safeIndex];
    }

    private void ClearAttackOffsets()
    {
        attackPivotOffset = Vector3.zero;
        attackUpperAngleOffset = 0f;
        attackLowerAngleOffset = 0f;
        attackLengthScaleOffset = 0f;
    }

    private void SetAttackPivotOffset(Vector3 desiredLocalOffset)
    {
        attackPivotOffset = desiredLocalOffset;
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
