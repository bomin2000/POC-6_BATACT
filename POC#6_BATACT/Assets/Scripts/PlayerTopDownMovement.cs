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
    [SerializeField] private bool allowAirDash = true;
    [SerializeField] private bool dashUsesMoveInputFirst = true;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.55f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.75f, 0.12f);
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool flipVisualByFacing = true;

    public bool IsGrounded { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public int FacingSign { get; private set; } = 1;

    private Rigidbody2D body;
    private Collider2D[] ownColliders;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private float horizontalInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float catchStabilizeTimer;
    private float externalControlLockTimer;
    private bool hasAirDash;
    private float defaultGravityScale;
    private bool hasAimFacingOverride;
    private int aimFacingSign = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ownColliders = GetComponents<Collider2D>();
        defaultGravityScale = body.gravityScale;

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
            TryStartDash();
        }

        jumpBufferTimer -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        UpdateVisualFacing();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        if (externalControlLockTimer > 0f)
        {
            ApplyBetterJumpGravity();
            return;
        }

        if (IsDashing)
        {
            TickDash();
            return;
        }

        ApplyHorizontalMovement();
        TryConsumeJump();
        ApplyBetterJumpGravity();
    }

    private void UpdateGrounded()
    {
        Vector2 center = groundCheck != null
            ? (Vector2)groundCheck.position
            : (Vector2)transform.position + groundCheckOffset;

        IsGrounded = HasExternalGroundHit(center, groundCheckSize);

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

    private void ApplyHorizontalMovement()
    {
        float targetVelocityX = horizontalInput * moveSpeed;
        if (catchStabilizeTimer > 0f)
        {
            targetVelocityX = 0f;
        }

        float control = IsGrounded ? 1f : airControlMultiplier;
        float rate = Mathf.Abs(targetVelocityX) > 0.01f ? acceleration : deceleration;
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, targetVelocityX, rate * control * Time.fixedDeltaTime);

        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);
    }

    private void TryConsumeJump()
    {
        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
        {
            return;
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
        else if (body.linearVelocity.y > 0.01f && !Input.GetKey(jumpKey))
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

        int dashSign = FacingSign;
        if (dashUsesMoveInputFirst && Mathf.Abs(horizontalInput) > 0.01f)
        {
            dashSign = horizontalInput > 0f ? 1 : -1;
        }

        dashTimer = dashDuration;
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
        body.gravityScale = defaultGravityScale;
        body.linearVelocity = new Vector2(0f, Mathf.Min(body.linearVelocity.y, jumpVelocity * 0.35f));
    }

    public void ApplyExternalKnockback(Vector2 impulse, float controlLockSeconds)
    {
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
