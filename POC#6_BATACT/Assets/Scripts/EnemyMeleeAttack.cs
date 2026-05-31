using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackWindup = 0.5f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float knockbackForce = 10f;

    [Header("Hitbox")]
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 1f);
    [SerializeField] private Vector2 attackBoxOffset = new Vector2(0.6f, 0f);
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private string playerTag = "Player";

    [Header("References")]
    [SerializeField] private EnemySideViewChaser chaser;
    [SerializeField] private Transform visualRoot;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsAttacking;
    [SerializeField] private float debugCooldownTimer;
    [SerializeField] private float debugWindupTimer;

    private Transform target;
    private float originalVisualY = -1f;

    private void Awake()
    {
        if (chaser == null) chaser = GetComponent<EnemySideViewChaser>();
        if (playerLayers.value == 0) playerLayers = 1 << LayerMask.NameToLayer("Player");
    }

    private void Start()
    {
        FindTarget();
        if (visualRoot != null)
        {
            originalVisualY = Mathf.Abs(visualRoot.localScale.y);
        }
    }

    private void Update()
    {
        if (debugCooldownTimer > 0f) debugCooldownTimer -= Time.deltaTime;

        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        if (debugIsAttacking)
        {
            debugWindupTimer -= Time.deltaTime;
            
            // Visual feedback: squish down as windup telegraph
            if (visualRoot != null && originalVisualY > 0f)
            {
                float progress = 1f - (debugWindupTimer / attackWindup);
                float squish = Mathf.Lerp(originalVisualY, originalVisualY * 0.7f, progress);
                Vector3 scale = visualRoot.localScale;
                scale.y = squish;
                visualRoot.localScale = scale;
            }

            if (debugWindupTimer <= 0f)
            {
                ExecuteAttack();
            }
            return;
        }

        if (debugCooldownTimer <= 0f)
        {
            float distanceX = Mathf.Abs(target.position.x - transform.position.x);
            // Ensure enemy is relatively on the same Y level to avoid attacking from too far above/below
            float distanceY = Mathf.Abs(target.position.y - transform.position.y);
            
            if (distanceX <= attackRange && distanceY <= 1.5f)
            {
                StartAttack();
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            target = player.transform;
            return;
        }
        
        DualBladeWeaponController playerWeapon = FindFirstObjectByType<DualBladeWeaponController>();
        if (playerWeapon != null)
        {
            target = playerWeapon.transform;
        }
    }

    private void StartAttack()
    {
        debugIsAttacking = true;
        debugWindupTimer = attackWindup;
        if (chaser != null) chaser.SetExternalStun(true);
    }

    private void ExecuteAttack()
    {
        debugIsAttacking = false;
        debugCooldownTimer = attackCooldown;
        if (chaser != null) chaser.SetExternalStun(false);

        // Reset visual squish
        if (visualRoot != null && originalVisualY > 0f)
        {
            Vector3 scale = visualRoot.localScale;
            scale.y = originalVisualY;
            visualRoot.localScale = scale;
        }

        int facingSign = visualRoot != null ? (int)Mathf.Sign(visualRoot.localScale.x) : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facingSign, attackBoxOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, playerLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(playerTag) || ((1 << hit.gameObject.layer) & playerLayers) != 0)
            {
                PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsHurtboxCollider(hit))
                {
                    Vector2 knockback = new Vector2(facingSign, 0.2f).normalized * knockbackForce;
                    playerHealth.TryTakeDamage(damage, knockback, gameObject);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = debugIsAttacking ? Color.red : new Color(1f, 0.6f, 0f, 0.5f);
        int facingSign = visualRoot != null ? (int)Mathf.Sign(visualRoot.localScale.x) : 1;
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facingSign, attackBoxOffset.y);
        Gizmos.DrawWireCube(center, attackBoxSize);
    }
}
