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

    [Header("Secondary Action Profiles")]
    [SerializeField] private WeaponHitboxProfile spearChargeProfile;
    [SerializeField] private WeaponHitboxProfile scissorsHoldProfile;
    [SerializeField] private WeaponHitboxProfile boomerangEmpoweredProfile;

    private bool hasPerfectCatchBuff = false;

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
    [SerializeField] private float perfectCatchInputWindow = 0.35f;
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

    [Header("Auto Attack (Click Hold)")]
    [SerializeField] private float spearAutoAttackInterval = 0.4f;
    [SerializeField] private float scissorsAutoAttackInterval = 0.5f;
    [SerializeField] private float boomerangAutoAttackInterval = 0.6f;

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

    public Vector2 AimDirection => aimDirection;

    public void ExecuteAssistAttack(bool secondary = false)
    {
        if (CurrentState == WeaponState.BareHand) return;

        lastAutoAttackTime = Time.time;
        // Removed consecutiveHitCount = 0 so assist attacks don't break the combo
        
        if (secondary)
        {
            FireSecondaryAttack();
        }
        else
        {
            FirePrimaryAttack();
        }
    }

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
    private WeaponHitboxProfile bufferedProfile;
    private PlayerTopDownMovement playerMovement;

    [Header("Auto Attack Status (Read-Only)")]
    [SerializeField] private float lastAutoAttackTime = -999f;
    [SerializeField] private bool canAutoAttack;

    [Header("Assist Systems")]
    [SerializeField] private WeaponTerrainConstraint2D terrainConstraint;

    [Header("Combo Hit Tracking")]
    [SerializeField] private int consecutiveHitCount = 0;
    [SerializeField] private int requiredHitsForSecondary = 2; // e.g. 2 hits -> 3rd attack is secondary

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
            meleeHitbox.OnHitSuccessful += HandleHitSuccessful;
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

    private void OnDestroy()
    {
        if (meleeHitbox != null)
        {
            meleeHitbox.OnHitSuccessful -= HandleHitSuccessful;
        }
    }

    private void HandleHitSuccessful(Collider2D target, WeaponHitboxProfile profile)
    {
        // Only count hits from normal profiles, not secondary charge/hold profiles
        if (profile == spearProfile || profile == scissorsProfile || profile == boomerangProfile)
        {
            consecutiveHitCount++;
        }
    }

    private TextMesh comboTextMesh;

    private void Update()
    {
        UpdateAimDirection();
        ReadFormInput();
        ReadAttackInput();
        SmoothSnapVisibleForm();
        UpdateComboIndicator();

        // Sync player swing state with boomerang anchor state (e.g. auto-release on timeout)
        if (playerMovement != null && playerMovement.IsRopeSwinging)
        {
            if (activeBoomerang == null || !activeBoomerang.IsAnchored)
            {
                playerMovement.LaunchFromSwing();
            }
        }
    }

    private void EnsureComboIndicator()
    {
        if (comboTextMesh == null)
        {
            GameObject go = new GameObject("ComboIndicator");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0f); // slightly higher than catch indicator
            
            comboTextMesh = go.AddComponent<TextMesh>();
            comboTextMesh.anchor = TextAnchor.MiddleCenter;
            comboTextMesh.alignment = TextAlignment.Center;
            comboTextMesh.fontSize = 60;
            comboTextMesh.characterSize = 0.12f;
            comboTextMesh.color = Color.white;
            
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sortingOrder = 35; // Put on top
        }
    }

    private void UpdateComboIndicator()
    {
        EnsureComboIndicator();

        if (consecutiveHitCount == 0)
        {
            comboTextMesh.text = "";
            return;
        }

        if (consecutiveHitCount >= requiredHitsForSecondary)
        {
            comboTextMesh.text = "스킬 준비 완료!";
            comboTextMesh.color = Color.Lerp(Color.yellow, Color.red, Mathf.PingPong(Time.time * 10f, 1f));
            float scale = 1f + Mathf.PingPong(Time.time * 5f, 0.2f);
            comboTextMesh.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            comboTextMesh.text = $"{consecutiveHitCount} 연타!";
            comboTextMesh.color = Color.white;
            comboTextMesh.transform.localScale = Vector3.one;
        }
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
            // Ignore boomerang select key if it's Mouse1 and we are in BareHand (to prevent overriding morph queue when using Rope launch)
            if (!(CurrentState == WeaponState.BareHand && boomerangSelectKey == KeyCode.Mouse1))
            {
                requestedState = WeaponState.Boomerang;
            }
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

    public bool CanAutoAttackNow
    {
        get
        {
            if (CurrentState == WeaponState.BareHand) return false;
            float currentInterval = 1f;
            switch (CurrentState)
            {
                case WeaponState.Spear: currentInterval = spearAutoAttackInterval; break;
                case WeaponState.Scissors: currentInterval = scissorsAutoAttackInterval; break;
                case WeaponState.Boomerang: currentInterval = boomerangAutoAttackInterval; break;
            }
            return Time.time >= lastAutoAttackTime + currentInterval;
        }
    }

    public void TryAutoAttackFromAssist()
    {
        if (CurrentState == WeaponState.BareHand) return;

        float currentInterval = 1f;
        switch (CurrentState)
        {
            case WeaponState.Spear: currentInterval = spearAutoAttackInterval; break;
            case WeaponState.Scissors: currentInterval = scissorsAutoAttackInterval; break;
            case WeaponState.Boomerang: currentInterval = boomerangAutoAttackInterval; break;
        }

        if (Time.time >= lastAutoAttackTime + currentInterval)
        {
            lastAutoAttackTime = Time.time;
            if (consecutiveHitCount >= requiredHitsForSecondary)
            {
                consecutiveHitCount = 0;
                FireSecondaryAttack();
            }
            else
            {
                FirePrimaryAttack();
            }
        }
    }

    private void ReadAttackInput()
    {
        bool isKeyDown = Input.GetKeyDown(attackKey);
        bool isRightClick = Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(boomerangSelectKey);
        bool isKeyHeld = Input.GetKey(attackKey); 

        if (CurrentState == WeaponState.BareHand)
        {
            canAutoAttack = false;

            // Handle Rope Actions while Anchored
            if (activeBoomerang != null && activeBoomerang.IsAnchored)
            {
                // Left Click: Zip to anchor
                if (isKeyDown)
                {
                    if (playerMovement != null && !playerMovement.IsRopeMoving)
                    {
                        playerMovement.OnRopeMoveFinished -= HandleRopeMoveFinished; // safety clear
                        playerMovement.OnRopeMoveFinished += HandleRopeMoveFinished;
                        playerMovement.StartRopeMove(activeBoomerang.AnchoredPosition);
                    }
                }
                // Right Click: Release anchor and fly
                else if (isRightClick)
                {
                    if (playerMovement != null && playerMovement.IsRopeSwinging)
                    {
                        // Launch the player with momentum
                        playerMovement.LaunchFromSwing();
                    }
                }
            }
            return;
        }

        float currentInterval = 1f;
        switch (CurrentState)
        {
            case WeaponState.Spear: currentInterval = spearAutoAttackInterval; break;
            case WeaponState.Scissors: currentInterval = scissorsAutoAttackInterval; break;
            case WeaponState.Boomerang: currentInterval = boomerangAutoAttackInterval; break;
        }

        canAutoAttack = Time.time >= lastAutoAttackTime + currentInterval;

        // Reset hit count if combo drops
        if (Time.time - lastComboTime > comboResetSeconds)
        {
            consecutiveHitCount = 0;
        }

        // Secondary attack handling via Right Click
        if (isRightClick)
        {
            if (consecutiveHitCount >= requiredHitsForSecondary)
            {
                consecutiveHitCount = 0;
                lastAutoAttackTime = Time.time;
                FireSecondaryAttack();
            }
        }
        // Primary attack handling via Left Click
        else if (isKeyDown || (canAutoAttack && isKeyHeld))
        {
            if (canAutoAttack) 
            {
                lastAutoAttackTime = Time.time;
            }
            
            if (consecutiveHitCount >= requiredHitsForSecondary)
            {
                consecutiveHitCount = 0;
                FireSecondaryAttack();
            }
            else
            {
                FirePrimaryAttack();
            }
        }
    }

    private void FirePrimaryAttack()
    {
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

    private void FireSecondaryAttack()
    {
        switch (CurrentState)
        {
            case WeaponState.Spear:
                if (spearChargeProfile != null)
                {
                    TryStartComboMeleeAttack(WeaponState.Spear, spearChargeProfile);
                }
                break;
            case WeaponState.Scissors:
                if (scissorsHoldProfile != null)
                {
                    TryStartComboMeleeAttack(WeaponState.Scissors, scissorsHoldProfile);
                }
                break;
            case WeaponState.Boomerang:
                ThrowBoomerang(true);
                break;
        }
    }

    private void TryStartComboMeleeAttack(WeaponState attackState, WeaponHitboxProfile profile)
    {
        if (enableComboBuffer && meleeAnimationRoutine != null)
        {
            hasBufferedAttack = true;
            bufferedAttackTime = Time.time;
            bufferedProfile = profile;
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
        PlayMeleeVisualAnimation(attackState, attackComboIndex, profile);
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
        Vector3 totalOffset = attackPivotOffset + constraintPivotOffset;
        if (weaponRoot == null)
        {
            return totalOffset;
        }

        return weaponRoot.TransformVector(totalOffset);
    }

    private void PlayMeleeVisualAnimation(WeaponState attackState, int attackComboIndex, WeaponHitboxProfile profile)
    {
        if (meleeAnimationRoutine != null)
        {
            StopCoroutine(meleeAnimationRoutine);
        }

        meleeAnimationRoutine = StartCoroutine(MeleeAnimationRoutine(attackState, attackComboIndex, profile));
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

    private void ThrowBoomerang(bool isEmpowered = false)
    {
        if (boomerangPrefab == null || activeBoomerang != null)
        {
            return;
        }

        lastComboTime = Time.time;

        WeaponHitboxProfile actualProfile = isEmpowered && boomerangEmpoweredProfile != null ? boomerangEmpoweredProfile : boomerangProfile;

        activeBoomerang = Instantiate(boomerangPrefab, throwSpawnPoint.position, Quaternion.identity);
        activeBoomerang.Caught += OnBoomerangCaught;
        activeBoomerang.OnHit += HandleHitSuccessful;
        activeBoomerang.OnAnchoredEvent += OnBoomerangAnchored;
        activeBoomerang.Launch(transform, actualProfile, aimDirection);

        TransitionTo(WeaponState.BareHand);
    }

    private void OnBoomerangAnchored(Vector3 anchorPos)
    {
        if (playerMovement != null)
        {
            playerMovement.OnRopeSwingCanceled -= HandleRopeSwingCanceled;
            playerMovement.OnRopeSwingCanceled += HandleRopeSwingCanceled;
            playerMovement.StartRopeSwing(anchorPos);
        }
    }

    private void HandleRopeSwingCanceled()
    {
        if (activeBoomerang != null)
        {
            activeBoomerang.ReleaseAnchor();
        }
    }

    private void HandleRopeMoveFinished()
    {
        if (playerMovement != null)
        {
            playerMovement.OnRopeMoveFinished -= HandleRopeMoveFinished;
        }

        if (activeBoomerang != null)
        {
            activeBoomerang.ReleaseAnchor();
        }
    }

    private void OnBoomerangCaught(DualBladeBoomerangProjectile projectile)
    {
        if (projectile != activeBoomerang)
        {
            return;
        }

        activeBoomerang.Caught -= OnBoomerangCaught;
        activeBoomerang.OnHit -= HandleHitSuccessful;
        activeBoomerang.OnAnchoredEvent -= OnBoomerangAnchored;
        Destroy(activeBoomerang.gameObject);
        activeBoomerang = null;

        if (playerMovement != null)
        {
            playerMovement.OnRopeSwingCanceled -= HandleRopeSwingCanceled;
            playerMovement.StabilizeAfterBoomerangCatch(catchStabilizeSeconds);
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

        if (terrainConstraint != null)
        {
            terrainConstraint.ResetStates();
        }

        ClearAttackOffsets();
        hasBufferedAttack = false;
        comboState = nextState;
        comboIndex = 0;
        CurrentState = nextState;
        lastAutoAttackTime = -999f;

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

    private Vector3 constraintPivotOffset;

    private void SmoothSnapVisibleForm()
    {
        if (CurrentState == WeaponState.BareHand || pivot == null)
        {
            return;
        }

        Vector3 targetPivot = GetPivotPosition(visibleForm) + attackPivotOffset;
        Vector3 originalTargetPivot = targetPivot;

        BladePose pose = GetPose(visibleForm);
        pose.upperBladeAngle += attackUpperAngleOffset;
        pose.lowerBladeAngle += attackLowerAngleOffset;
        pose.lengthScale += attackLengthScaleOffset;

        if (terrainConstraint != null)
        {
            terrainConstraint.ApplyConstraint(ref aimDirection, ref targetPivot, ref pose.lengthScale, visibleForm);
        }
        
        constraintPivotOffset = targetPivot - originalTargetPivot;
        
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

    private IEnumerator MeleeAnimationRoutine(WeaponState attackState, int attackComboIndex, WeaponHitboxProfile profile)
    {
        ClearAttackOffsets();
        bool isCharge = (profile == spearChargeProfile);
        bool isHold = (profile == scissorsHoldProfile);

        if (attackState == WeaponState.Spear)
        {
            yield return SpearThrustAnimation(attackComboIndex, isCharge);
        }
        else if (attackState == WeaponState.Scissors)
        {
            yield return ScissorsCutAnimation(attackComboIndex, isHold);
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

        WeaponHitboxProfile profile = bufferedProfile != null ? bufferedProfile : (attackState == WeaponState.Spear ? spearProfile : scissorsProfile);
        int nextComboIndex = ResolveNextComboIndex(attackState);
        StartMeleeAttack(attackState, profile, nextComboIndex);
        bufferedProfile = null;
    }

    private IEnumerator SpearThrustAnimation(int attackComboIndex, bool isCharge)
    {
        float distanceMultiplier = GetComboArrayValue(spearThrustDistanceMultipliers, attackComboIndex, 1f);
        float stretchMultiplier = GetComboArrayValue(spearStretchMultipliers, attackComboIndex, 1f);
        
        if (isCharge)
        {
            distanceMultiplier *= 3.0f;
            stretchMultiplier *= 2.5f;
        }

        float thrustDistance = spearThrustDistance * distanceMultiplier;
        float thrustStretch = spearThrustStretch * stretchMultiplier;
        
        float outSeconds = isCharge ? spearThrustOutSeconds * 1.5f : spearThrustOutSeconds;
        float backSeconds = isCharge ? spearThrustBackSeconds * 2.0f : spearThrustBackSeconds;

        float elapsed = 0f;
        while (elapsed < outSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / outSeconds));
            SetAttackPivotOffset(Vector3.right * (thrustDistance * t));
            attackLengthScaleOffset = thrustStretch * t;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < backSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / backSeconds));
            SetAttackPivotOffset(Vector3.right * Mathf.Lerp(thrustDistance, 0f, t));
            attackLengthScaleOffset = Mathf.Lerp(thrustStretch, 0f, t);
            yield return null;
        }
    }

    private IEnumerator ScissorsCutAnimation(int attackComboIndex, bool isHold)
    {
        BladePose basePose = scissorsPose;
        float closeAngle = GetComboArrayValue(scissorsCloseAngleByCombo, attackComboIndex, scissorsCutCloseAngle);
        float lungeDistance = scissorsCutLungeDistance * GetComboArrayValue(scissorsLungeMultipliers, attackComboIndex, 1f);
        float closeUpperOffset = closeAngle - basePose.upperBladeAngle;
        float closeLowerOffset = -closeAngle - basePose.lowerBladeAngle;

        if (isHold)
        {
            float holdDuration = 0.4f;
            float elapsedHold = 0f;
            while (elapsedHold < holdDuration)
            {
                elapsedHold += Time.unscaledDeltaTime;
                // Rapidly snip the scissors
                float tHold = Mathf.PingPong(elapsedHold * 15f, 1f);
                attackUpperAngleOffset = closeUpperOffset * tHold * 1.5f;
                attackLowerAngleOffset = closeLowerOffset * tHold * 1.5f;
                SetAttackPivotOffset(Vector3.right * (lungeDistance * 2f * tHold));
                
                // Visually stretch the blades to emphasize the 360 AoE
                attackLengthScaleOffset = 1.5f; 
                yield return null;
            }
            yield break;
        }

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
