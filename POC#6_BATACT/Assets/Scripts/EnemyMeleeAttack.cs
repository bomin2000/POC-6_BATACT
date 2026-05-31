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

    [Header("Visual Punch (Optional)")]
    [SerializeField] private Transform attackVisual;
    [SerializeField] private float windupPullback = -0.2f;
    [SerializeField] private float punchExtension = 0.5f;
    [SerializeField] private float visualReturnSpeed = 15f;
    [SerializeField] private float punchStretchMultiplier = 1.5f;

    [Header("References")]
    [SerializeField] private EnemySideViewChaser chaser;
    [SerializeField] private Transform visualRoot;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool debugIsAttacking;
    [SerializeField] private float debugWindupTimer;
    [SerializeField] private float debugCooldownTimer;
    [SerializeField] private float debugPlayerDistance;
    [SerializeField] private bool debugCanAttack;

    public bool IsAttacking => debugIsAttacking;
    public bool IsInAttackRange => target != null && Mathf.Abs(target.position.x - transform.position.x) <= attackRange;

    private Transform target;
    private float originalVisualY = -1f;
    private Vector3 originalAttackVisualScale;
    private Vector3 originalAttackVisualPos;
    private Vector3 targetArmPos;

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
        if (attackVisual != null)
        {
            originalAttackVisualScale = attackVisual.localScale;
            originalAttackVisualPos = attackVisual.localPosition;
            targetArmPos = originalAttackVisualPos;
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

            // Arm windup (pull back)
            if (attackVisual != null)
            {
                targetArmPos = originalAttackVisualPos + new Vector3(windupPullback, 0f, 0f);
                attackVisual.localPosition = Vector3.Lerp(attackVisual.localPosition, targetArmPos, Time.deltaTime * 15f);
            }

            if (debugWindupTimer <= 0f)
            {
                ExecuteAttack();
            }
        }
        else
        {
            // Restore visual and arm
            if (attackVisual != null)
            {
                targetArmPos = originalAttackVisualPos;
                attackVisual.localPosition = Vector3.Lerp(attackVisual.localPosition, targetArmPos, Time.deltaTime * visualReturnSpeed);
                attackVisual.localScale = Vector3.Lerp(attackVisual.localScale, originalAttackVisualScale, Time.deltaTime * visualReturnSpeed);
            }
        }

        if (debugIsAttacking) return;

        float distanceX = Mathf.Abs(target.position.x - transform.position.x);
        float distanceY = Mathf.Abs(target.position.y - transform.position.y);
        
        debugPlayerDistance = distanceX;
        debugCanAttack = (debugCooldownTimer <= 0f && distanceX <= attackRange && distanceY <= 1.5f);

        if (debugCanAttack)
        {
            StartAttack();
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

        // Punch arm visually
        if (attackVisual != null)
        {
            attackVisual.localPosition = originalAttackVisualPos + new Vector3(punchExtension, 0f, 0f);
            Vector3 stretchScale = originalAttackVisualScale;
            stretchScale.x *= punchStretchMultiplier;
            attackVisual.localScale = stretchScale;
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
