using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public sealed class PlayerTopDownMovement : MonoBehaviour
{
    [Header("Side View Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 70f;
    [SerializeField] private float deceleration = 90f;
    [SerializeField] private float airControlMultiplier = 0.65f;

    [Header("Jump")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private float jumpVelocity = 13f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.2f;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.45f;
    [SerializeField] private float dashBufferTime = 0.1f;
    [SerializeField] private bool allowAirDash = true;
    [SerializeField] private bool dashUsesMoveInputFirst = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.75f, 0.12f);
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private bool useColliderGroundFallback = true;
    [SerializeField] private float groundProbeDistance = 0.08f;
    [SerializeField] private float minimumGroundNormalY = 0.45f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool flipVisualByFacing = true;

    public bool IsGrounded { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public int FacingSign { get; private set; } = 1;

    private Rigidbody2D body;
    private CapsuleCollider2D bodyCollider;
    private Collider2D[] ownColliders;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private readonly RaycastHit2D[] groundCastHits = new RaycastHit2D[8];
    private ContactFilter2D groundContactFilter;
    private float horizontalInput;
    private float verticalInput;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float dashBufferTimer;
    private float dashCooldownTimer;
    private float catchStabilizeTimer;
    private float externalControlLockTimer;
    private bool hasAirDash;
    private float defaultGravityScale;
    private bool hasAimFacingOverride;
    private int aimFacingSign = 1;

    // Rope Move Fields
    private bool isRopeMoving;
    private Vector2 ropeTargetPos;
    [Header("Rope Move")]
    [SerializeField] private float ropeMoveSpeed = 35f;
    [SerializeField] private float ropeArrivalDistance = 1.0f;
    public bool IsRopeMoving => isRopeMoving;
    public System.Action OnRopeMoveFinished;

    // Rope Swing Fields
    private DistanceJoint2D ropeSwingJoint;
    public bool IsRopeSwinging => ropeSwingJoint != null && ropeSwingJoint.enabled;
    public System.Action OnRopeSwingCanceled;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        defaultGravityScale = body.gravityScale;
        ConfigureGroundContactFilter();

        if (defaultGravityScale <= 0f)
        {
            defaultGravityScale = 3f;
            body.gravityScale = defaultGravityScale;
        }

        body.freezeRotation = true;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (catchStabilizeTimer > 0f)
        {
            catchStabilizeTimer -= Time.deltaTime;
        }

        if (externalControlLockTimer > 0f)
        {
            externalControlLockTimer -= Time.deltaTime;
        }

        if (hasAimFacingOverride)
        {
            FacingSign = aimFacingSign;
        }
        else if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            FacingSign = horizontalInput > 0f ? 1 : -1;
        }

        if (Input.GetKeyDown(jumpKey))
        {
            jumpBufferTimer = jumpBufferTime;
        }

        if (Input.GetKeyDown(dashKey))
        {
            dashBufferTimer = dashBufferTime;
        }

        jumpBufferTimer -= Time.deltaTime;
        dashBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        UpdateVisualFacing();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        if (isRopeMoving)
        {
            TickRopeMove();
            return;
        }

        if (dashBufferTimer > 0f)
        {
            TryStartDash();
        }

        if (externalControlLockTimer > 0f)
        {
            if (catchStabilizeTimer > 0f)
            {
                // Actively clamp runaway physics momentum even if external lock is somehow active
                float clampedX = Mathf.Clamp(body.linearVelocity.x, -moveSpeed * 1.2f, moveSpeed * 1.2f);
                body.linearVelocity = new Vector2(clampedX, body.linearVelocity.y);
            }
            ApplyBetterJumpGravity();
            return;
        }

        if (IsDashing)
        {
            TickDash();
            return;
        }

        if (IsRopeSwinging)
        {
            if (Mathf.Abs(verticalInput) > 0.01f && ropeSwingJoint != null)
            {
                float newDistance = ropeSwingJoint.distance - (verticalInput * 10f * Time.fixedDeltaTime);
                ropeSwingJoint.distance = Mathf.Clamp(newDistance, 1.0f, 15f);
            }

            if (IsGrounded)
            {
                // If grounded, allow normal walking (constrained by max distance)
                ApplyHorizontalMovement();
            }
            else
            {
                TickRopeSwingMovement();
            }
            TryConsumeJump();
            return;
        }

        ApplyHorizontalMovement();
        TryConsumeJump();
        ApplyBetterJumpGravity();
    }

    private void TickRopeSwingMovement()
    {
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            // Apply force to swing left/right instead of setting velocity directly
            body.AddForce(Vector2.right * horizontalInput * acceleration * 0.35f);
        }
        
        ApplyBetterJumpGravity();
    }

    public void StartRopeSwing(Vector2 anchorPos)
    {
        if (ropeSwingJoint == null)
        {
            ropeSwingJoint = gameObject.AddComponent<DistanceJoint2D>();
            ropeSwingJoint.enableCollision = true;
            ropeSwingJoint.maxDistanceOnly = true;
            ropeSwingJoint.autoConfigureDistance = false;
            ropeSwingJoint.autoConfigureConnectedAnchor = false;
        }

        ropeSwingJoint.autoConfigureConnectedAnchor = false;
        ropeSwingJoint.connectedAnchor = anchorPos;
        ropeSwingJoint.distance = Vector2.Distance(body.position, anchorPos);
        ropeSwingJoint.enabled = true;
    }

    public void StopRopeSwing(bool invokeEvent = true)
    {
        if (ropeSwingJoint != null && ropeSwingJoint.enabled)
        {
            ropeSwingJoint.enabled = false;
            if (invokeEvent)
            {
                OnRopeSwingCanceled?.Invoke();
            }
        }
    }

    public void LaunchFromSwing()
    {
        if (ropeSwingJoint == null || !ropeSwingJoint.enabled) return;

        Vector2 currentVel = body.linearVelocity;
        
        StopRopeSwing(true);
        
        // Boost momentum to make the swing launch feel good
        currentVel *= 1.35f; 

        // Apply external lock so that horizontal input doesn't instantly kill the velocity
        externalControlLockTimer = 0.4f; 
        
        body.linearVelocity = currentVel;
        body.gravityScale = defaultGravityScale;
    }

    public void StartRopeMove(Vector2 targetPos)
    {
        StopRopeSwing(false); // Don't trigger cancellation event, we are transitioning to Zip
        isRopeMoving = true;
        ropeTargetPos = targetPos;
        body.gravityScale = 0f; // Turn off gravity while pulling
        dashTimer = 0f; // Cancel dashes
    }

    public void CancelRopeMove()
    {
        if (isRopeMoving)
        {
            isRopeMoving = false;
            body.gravityScale = defaultGravityScale;
            OnRopeMoveFinished?.Invoke();
        }
    }

    private void TickRopeMove()
    {
        Vector2 currentPos = body.position;
        float dist = Vector2.Distance(currentPos, ropeTargetPos);

        if (dist <= 1.0f)
        {
            // Arrived
            CancelRopeMove();
            // Kill velocity
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 dir = (ropeTargetPos - currentPos).normalized;
            body.linearVelocity = dir * ropeMoveSpeed;
            // Face direction of travel
            FacingSign = dir.x >= 0f ? 1 : -1;
        }
    }

    private void UpdateGrounded()
    {
        Vector2 center = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + groundCheckOffset;

        IsGrounded = HasExternalGroundHit(center, groundCheckSize) || HasGroundCastHit();

        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
            hasAirDash = true;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
    }

    private void ConfigureGroundContactFilter()
    {
        groundContactFilter = new ContactFilter2D();
        groundContactFilter.SetLayerMask(groundLayers);
        groundContactFilter.useTriggers = false;
    }

    private bool HasExternalGroundHit(Vector2 center, Vector2 size)
    {
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, groundHits, groundLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = groundHits[i];
            if (hit != null && !hit.isTrigger && !IsOwnCollider(hit))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasGroundCastHit()
    {
        if (!useColliderGroundFallback || bodyCollider == null)
        {
            return false;
        }

        int count = bodyCollider.Cast(Vector2.down, groundContactFilter, groundCastHits, groundProbeDistance);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = groundCastHits[i];
            if (hit.collider == null || hit.collider.isTrigger || IsOwnCollider(hit.collider))
            {
                continue;
            }

            if (hit.normal.y >= minimumGroundNormalY)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsOwnCollider(Collider2D hit)
    {
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (hit == ownColliders[i])
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 weaponPushDelta;

    public void ApplyWeaponPushDelta(Vector2 delta)
    {
        weaponPushDelta += delta;
    }

    private bool restrictLeftMovement;
    private bool restrictRightMovement;

    public void SetMovementRestrictions(bool restrictLeft, bool restrictRight)
    {
        restrictLeftMovement = restrictLeft;
        restrictRightMovement = restrictRight;
    }

    private void ApplyHorizontalMovement()
    {
        float targetVelocityX = horizontalInput * moveSpeed;
        if (catchStabilizeTimer > 0f)
        {
            targetVelocityX = 0f;
            // Forcefully crush horizontal momentum so the player doesn't slide 
            // and interpret the slide as a "boomerang recoil" push.
            float dampenedX = Mathf.MoveTowards(body.linearVelocity.x, 0f, moveSpeed * 20f * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(dampenedX, body.linearVelocity.y);
        }

        if (restrictLeftMovement && targetVelocityX < 0f)
        {
            targetVelocityX = 0f;
        }
        if (restrictRightMovement && targetVelocityX > 0f)
        {
            targetVelocityX = 0f;
        }

        float control = IsGrounded ? 1f : airControlMultiplier;
        float rate = Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : deceleration;
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, targetVelocityX, rate * control * Time.fixedDeltaTime);

        if (restrictLeftMovement && nextVelocityX < 0f)
        {
            nextVelocityX = 0f;
        }
        if (restrictRightMovement && nextVelocityX > 0f)
        {
            nextVelocityX = 0f;
        }

        // Apply weapon push delta to position and kill opposing velocity
        if (weaponPushDelta.sqrMagnitude > 0f)
        {
            body.position += weaponPushDelta;

            // Kill velocity that opposes the push direction
            if (Vector2.Dot(body.linearVelocity, weaponPushDelta) < 0f)
            {
                if (Mathf.Abs(weaponPushDelta.x) > 0.01f && Mathf.Sign(body.linearVelocity.x) != Mathf.Sign(weaponPushDelta.x))
                {
                    nextVelocityX = 0f;
                }
                if (weaponPushDelta.y > 0.01f && body.linearVelocity.y < 0f)
                {
                    body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
                }
            }
            weaponPushDelta = Vector2.zero;
        }

        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);
    }

    private void TryConsumeJump()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
        {
            return;
        }

        if (isRopeMoving)
        {
            CancelRopeMove();
        }

        if (IsRopeSwinging)
        {
            StopRopeSwing();
        }

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
    }

    private void ApplyBetterJumpGravity()
    {
        if (body.linearVelocity.y < -0.01f)
        {
            body.gravityScale = defaultGravityScale * fallGravityMultiplier;
        }
        else if (body.linearVelocity.y > 0.01f && (!Input.GetKey(jumpKey) && externalControlLockTimer <= 0f))
        {
            body.gravityScale = defaultGravityScale * lowJumpGravityMultiplier;
        }
        else
        {
            body.gravityScale = defaultGravityScale;
        }
    }

    private void TryStartDash()
    {
        if (dashCooldownTimer > 0f || IsDashing || catchStabilizeTimer > 0f || externalControlLockTimer > 0f)
        {
            return;
        }

        if (!IsGrounded)
        {
            if (!allowAirDash || !hasAirDash)
            {
                return;
            }

            hasAirDash = false;
        }

        if (isRopeMoving)
        {
            CancelRopeMove();
        }

        if (IsRopeSwinging)
        {
            StopRopeSwing();
        }


        int dashSign = FacingSign;
        if (dashUsesMoveInputFirst && Mathf.Abs(horizontalInput) > 0.01f)
        {
            dashSign = horizontalInput > 0f ? 1 : -1;
        }

        dashTimer = dashDuration;
        dashBufferTimer = 0f;
        dashCooldownTimer = dashCooldown;
        body.gravityScale = 0f;
        body.linearVelocity = new Vector2(dashSign * dashSpeed, 0f);
        FacingSign = dashSign;
    }

    private void TickDash()
    {
        if (catchStabilizeTimer > 0f)
        {
            dashTimer = 0f;
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        dashTimer -= Time.fixedDeltaTime;
        body.linearVelocity = new Vector2(FacingSign * dashSpeed, 0f);

        if (dashTimer <= 0f)
        {
            body.gravityScale = defaultGravityScale;
            body.linearVelocity = new Vector2(FacingSign * moveSpeed, 0f);
        }
    }

    private void UpdateVisualFacing()
    {
        if (!flipVisualByFacing || visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingSign;
        visualRoot.localScale = scale;
    }

    public void StabilizeAfterBoomerangCatch(float seconds)
    {
        catchStabilizeTimer = Mathf.Max(catchStabilizeTimer, seconds);
        dashTimer = 0f;
        dashCooldownTimer = Mathf.Max(dashCooldownTimer, 0.08f);
        externalControlLockTimer = 0f; // Clear any errant overlap knockbacks
        
        body.gravityScale = defaultGravityScale;
        
        // Kill horizontal velocity completely so the player feels a firm "catch" rather than a slide
        body.linearVelocity = new Vector2(0f, Mathf.Min(body.linearVelocity.y, jumpVelocity * 0.35f));
    }

    public void ApplyExternalKnockback(Vector2 impulse, float controlLockSeconds)
    {
        if (isRopeMoving)
        {
            CancelRopeMove();
        }

        if (IsRopeSwinging)
        {
            StopRopeSwing();
        }

        externalControlLockTimer = Mathf.Max(externalControlLockTimer, controlLockSeconds);
        dashTimer = 0f;
        body.gravityScale = defaultGravityScale;
        body.linearVelocity = Vector2.zero;
        body.AddForce(impulse, ForceMode2D.Impulse);
    }

    public void SetAimFacingSign(int sign)
    {
        hasAimFacingOverride = true;
        aimFacingSign = sign >= 0 ? 1 : -1;
    }

    public void ClearAimFacingOverride()
    {
        hasAimFacingOverride = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + groundCheckOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, groundCheckSize);
    }
}
