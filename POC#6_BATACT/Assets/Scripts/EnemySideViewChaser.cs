using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemySideViewChaser : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float stopDistance = 0.85f;
    [SerializeField] private float acceleration = 35f;

    [Header("Ground")]
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -1f);
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.12f);
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Obstacle Hop")]
    [SerializeField] private bool hopWhenBlocked = true;
    [SerializeField] private Vector2 wallCheckOffset = new Vector2(0.55f, -0.15f);
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.18f, 0.8f);
    [SerializeField] private float hopVelocity = 8f;
    [SerializeField] private float hopCooldown = 0.7f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody2D body;
    private Collider2D[] ownColliders;
    private readonly Collider2D[] overlapHits = new Collider2D[8];
    private float hopCooldownTimer;
    private int facingSign = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ownColliders = GetComponents<Collider2D>();
        body.freezeRotation = true;

        if (body.gravityScale <= 0f)
        {
            body.gravityScale = 3f;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target == null)
        {
            DualBladeWeaponController playerWeapon = FindFirstObjectByType<DualBladeWeaponController>();
            if (playerWeapon != null)
            {
                target = playerWeapon.transform;
            }
        }
    }

    private void Update()
    {
        hopCooldownTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            Decelerate();
            return;
        }

        float deltaX = target.position.x - transform.position.x;
        float distanceX = Mathf.Abs(deltaX);

        if (distanceX > 0.05f)
        {
            facingSign = deltaX > 0f ? 1 : -1;
        }

        if (distanceX <= stopDistance)
        {
            Decelerate();
            UpdateVisualFacing();
            return;
        }

        float desiredVelocityX = facingSign * moveSpeed;
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, desiredVelocityX, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);

        if (hopWhenBlocked && IsGrounded() && IsBlockedAhead() && hopCooldownTimer <= 0f)
        {
            hopCooldownTimer = hopCooldown;
            body.linearVelocity = new Vector2(body.linearVelocity.x, hopVelocity);
        }

        UpdateVisualFacing();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Decelerate()
    {
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, 0f, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(nextVelocityX, body.linearVelocity.y);
    }

    private bool IsGrounded()
    {
        Vector2 center = (Vector2)transform.position + groundCheckOffset;
        return HasExternalOverlap(center, groundCheckSize);
    }

    private bool IsBlockedAhead()
    {
        Vector2 center = (Vector2)transform.position + new Vector2(wallCheckOffset.x * facingSign, wallCheckOffset.y);
        return HasExternalOverlap(center, wallCheckSize);
    }

    private bool HasExternalOverlap(Vector2 center, Vector2 size)
    {
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, overlapHits, groundLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapHits[i];
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

    private void UpdateVisualFacing()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * facingSign;
        visualRoot.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + new Vector2(wallCheckOffset.x * facingSign, wallCheckOffset.y), wallCheckSize);
    }
}
