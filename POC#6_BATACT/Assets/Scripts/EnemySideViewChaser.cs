using System.Collections.Generic;
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
    [SerializeField] private bool ignoreEnemyToEnemyCollision = true;

    [Header("Crowd Separation")]
    [SerializeField] private LayerMask enemyLayers = 1 << 3;
    [SerializeField] private float separationRadius = 0.85f;
    [SerializeField] private float separationStrength = 1.4f;

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
    private readonly Collider2D[] separationHits = new Collider2D[12];
    private float hopCooldownTimer;
    private int facingSign = 1;
    private bool externallyStunned;
    private static readonly List<EnemySideViewChaser> ActiveEnemies = new List<EnemySideViewChaser>();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        ownColliders = GetComponentsInChildren<Collider2D>();
        body.freezeRotation = true;

        if (body.gravityScale <= 0f)
        {
            body.gravityScale = 3f;
        }

        if (ignoreEnemyToEnemyCollision)
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
            ApplyLayerToChildren(gameObject.layer);
        }
    }

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }

        IgnoreAllActiveEnemyCollisions();
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
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

        if (externallyStunned)
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

        float desiredVelocityX = facingSign * moveSpeed + GetSeparationVelocityX();
        float nextVelocityX = Mathf.MoveTowards(body.linearVelocity.x, desiredVelocityX, acceleration * Time.fixedDeltaTime);
        body.linearVelocity = new Vector2(nextVelocityX, ClampVerticalEnemyStackVelocity(body.linearVelocity.y));

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

    public void SetExternalStun(bool stunned)
    {
        externallyStunned = stunned;

        if (stunned)
        {
            Decelerate();
        }
    }

    public void RefreshEnemyCollisionIgnores()
    {
        IgnoreAllActiveEnemyCollisions();
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

    private float ClampVerticalEnemyStackVelocity(float velocityY)
    {
        if (velocityY <= 0f || !IsEnemyBelowOrOverlapping())
        {
            return velocityY;
        }

        return Mathf.Min(velocityY, 0.5f);
    }

    private bool IsEnemyBelowOrOverlapping()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, separationHits, enemyLayers);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null || IsOwnCollider(hit))
            {
                continue;
            }

            float verticalDelta = hit.bounds.center.y - transform.position.y;
            if (verticalDelta < 0.6f)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyLayerToChildren(int layer)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.layer = layer;
        }
    }

    private void IgnoreAllActiveEnemyCollisions()
    {
        if (!ignoreEnemyToEnemyCollision)
        {
            return;
        }

        for (int i = 0; i < ActiveEnemies.Count; i++)
        {
            EnemySideViewChaser other = ActiveEnemies[i];
            if (other == null || other == this)
            {
                continue;
            }

            IgnoreCollisionWith(other);
            other.IgnoreCollisionWith(this);
        }
    }

    private void IgnoreCollisionWith(EnemySideViewChaser other)
    {
        if (other == null || ownColliders == null || other.ownColliders == null)
        {
            return;
        }

        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] == null)
            {
                continue;
            }

            for (int j = 0; j < other.ownColliders.Length; j++)
            {
                if (other.ownColliders[j] != null)
                {
                    Physics2D.IgnoreCollision(ownColliders[i], other.ownColliders[j], true);
                }
            }
        }
    }

    private float GetSeparationVelocityX()
    {
        if (separationRadius <= 0f || separationStrength <= 0f)
        {
            return 0f;
        }

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, separationHits, enemyLayers);
        float push = 0f;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = separationHits[i];
            if (hit == null || IsOwnCollider(hit))
            {
                continue;
            }

            float deltaX = transform.position.x - hit.transform.position.x;
            if (Mathf.Abs(deltaX) < 0.001f)
            {
                deltaX = Random.value < 0.5f ? -0.1f : 0.1f;
            }

            float weight = 1f - Mathf.Clamp01(Mathf.Abs(deltaX) / separationRadius);
            push += Mathf.Sign(deltaX) * weight * separationStrength;
        }

        return push;
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
