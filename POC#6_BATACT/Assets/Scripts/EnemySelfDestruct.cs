using UnityEngine;
using System.Collections;

public class EnemySelfDestruct : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float triggerRange = 1.5f;
    [SerializeField] private float windupSeconds = 0.8f;
    [SerializeField] private bool stopDuringWindup = true;
    [SerializeField] private bool explodeOnDeath = false;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionDamage = 30f;
    [SerializeField] private float explosionKnockback = 15f;
    [SerializeField] private LayerMask playerLayers;
    [SerializeField] private string playerTag = "Player";

    [Header("References")]
    [SerializeField] private EnemySideViewChaser chaser;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private PrototypeEnemyHitReceiver hitReceiver;

    [Header("Debug Status (Read-Only)")]
    [SerializeField] private bool isWindingUp;
    [SerializeField] private bool hasExploded;

    private Transform target;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Vector3 originalScale;

    private void Awake()
    {
        if (chaser == null) chaser = GetComponent<EnemySideViewChaser>();
        if (hitReceiver == null) hitReceiver = GetComponent<PrototypeEnemyHitReceiver>();
        if (playerLayers.value == 0) playerLayers = 1 << LayerMask.NameToLayer("Player");

        if (visualRoot != null)
        {
            originalScale = visualRoot.localScale;
            renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
            }
        }
    }

    private void Start()
    {
        FindTarget();
        if (hitReceiver != null && explodeOnDeath)
        {
            hitReceiver.Died.AddListener(OnDeath);
        }
    }

    private void OnDestroy()
    {
        if (hitReceiver != null && explodeOnDeath)
        {
            hitReceiver.Died.RemoveListener(OnDeath);
        }
    }

    private void Update()
    {
        if (hasExploded || isWindingUp) return;

        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= triggerRange)
        {
            StartCoroutine(WindupRoutine());
        }
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

    private IEnumerator WindupRoutine()
    {
        isWindingUp = true;
        if (stopDuringWindup && chaser != null)
        {
            chaser.SetExternalStun(true);
        }

        float timer = 0f;
        bool toggle = false;

        while (timer < windupSeconds)
        {
            timer += Time.deltaTime;

            // Rapid blinking effect
            float blinkSpeed = Mathf.Lerp(10f, 40f, timer / windupSeconds);
            toggle = Mathf.Sin(timer * blinkSpeed) > 0;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].color = toggle ? Color.red : Color.white;
                }
            }

            // Pulse size effect
            if (visualRoot != null)
            {
                float pulse = 1f + (toggle ? 0.15f : 0f);
                visualRoot.localScale = new Vector3(Mathf.Sign(originalScale.x) * Mathf.Abs(originalScale.x) * pulse, Mathf.Abs(originalScale.y) * pulse, 1f);
            }

            yield return null;
        }

        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Reset visual back to normal before destroying
        if (visualRoot != null)
        {
            visualRoot.localScale = originalScale;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].color = originalColors[i];
                }
            }
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag(playerTag) || ((1 << hit.gameObject.layer) & playerLayers) != 0)
            {
                PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsHurtboxCollider(hit))
                {
                    Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    if (knockbackDir == Vector2.zero) knockbackDir = Vector2.up;

                    playerHealth.TryTakeDamage(explosionDamage, knockbackDir * explosionKnockback, gameObject);
                }
            }
        }

        // Kill self
        if (hitReceiver != null && !hitReceiver.IsDead)
        {
            hitReceiver.Kill(); // Force kill
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDeath()
    {
        if (!hasExploded)
        {
            StopAllCoroutines();
            Explode();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);

        Gizmos.color = isWindingUp ? Color.red : new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
